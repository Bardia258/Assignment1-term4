import Foundation
import Combine

struct LoginRequest: Encodable {
    let username: String
    let password: String
}

struct RegisterRequest: Encodable {
    let username: String
    let email: String
    let password: String
    let fullName: String
}

struct AuthResponse: Decodable {
    let token: String
}

@MainActor
class AuthViewModel: ObservableObject {
    @Published var email = ""
    @Published var password = ""
    @Published var name = ""
    
    @Published var isLoading = false
    @Published var errorMessage: String? = nil
    @Published private(set) var accountCreated = false

    private let api: APIClient
    private var registeredCredentials: LoginRequest?

    init(api: APIClient = .shared) {
        self.api = api
    }
    
    private var trimmedEmail: String { email.trimmingCharacters(in: .whitespacesAndNewlines) }

    var isLoginValid: Bool {
        !trimmedEmail.isEmpty && trimmedEmail.contains("@") && !password.isEmpty
    }
    
    var isRegisterValid: Bool {
        !name.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty
            && trimmedEmail.count <= 100 && isLoginValid && password.count >= 6
    }
    
    func login() async -> String? {
        guard !isLoading else { return nil }
        isLoading = true
        errorMessage = nil
        
        defer { isLoading = false }
        do {
            // The backend accepts an email in its username field.
            return try await authenticate(LoginRequest(username: trimmedEmail, password: password))
        } catch {
            errorMessage = error.localizedDescription
            return nil
        }
    }
    
    func register() async -> String? {
        guard !isLoading else { return nil }
        isLoading = true
        errorMessage = nil
        
        defer { isLoading = false }
        do {
            if registeredCredentials == nil {
                // Capture the credentials before either request suspends.
                let request = RegisterRequest(username: trimmedEmail, email: trimmedEmail,
                    password: password, fullName: name.trimmingCharacters(in: .whitespacesAndNewlines))
                try await api.register(request)
                registeredCredentials = LoginRequest(username: request.username, password: request.password)
                accountCreated = true
            }
            // Retry only login if account creation succeeded but sign-in failed.
            guard let registeredCredentials else { return nil }
            return try await authenticate(registeredCredentials)
        } catch {
            errorMessage = accountCreated
                ? "Your account was created, but sign-in did not finish. Tap Finish Signing In to retry. \(error.localizedDescription)"
                : error.localizedDescription
            return nil
        }
    }

    private func authenticate(_ request: LoginRequest) async throws -> String {
        let response = try await api.login(request)
        guard !response.token.isEmpty else {
            throw APIError(statusCode: 0, message: "The server did not return a login token.")
        }
        return response.token
    }
}
