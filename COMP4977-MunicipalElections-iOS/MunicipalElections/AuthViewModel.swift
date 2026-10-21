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
    
    private var trimmedEmail: String { email.trimmingCharacters(in: .whitespacesAndNewlines) }

    var isLoginValid: Bool {
        !trimmedEmail.isEmpty && trimmedEmail.contains("@") && !password.isEmpty
    }
    
    var isRegisterValid: Bool {
        !name.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty
            && trimmedEmail.count <= 100 && isLoginValid && password.count >= 6
    }
    
    func login() async -> String? {
        isLoading = true
        errorMessage = nil
        
        defer { isLoading = false }
        do {
            // The backend accepts an email in its username field.
            let response = try await APIClient.shared.login(LoginRequest(username: trimmedEmail, password: password))
            guard !response.token.isEmpty else {
                throw APIError(statusCode: 0, message: "The server did not return a login token.")
            }
            return response.token
        } catch {
            errorMessage = error.localizedDescription
            return nil
        }
    }
    
    func register() async -> Bool {
        isLoading = true
        errorMessage = nil
        
        defer { isLoading = false }
        do {
            // Use the email as the username to keep the existing form simple.
            try await APIClient.shared.register(RegisterRequest(username: trimmedEmail, email: trimmedEmail,
                password: password, fullName: name.trimmingCharacters(in: .whitespacesAndNewlines)))
            return true
        } catch {
            errorMessage = error.localizedDescription
            return false
        }
    }
}
