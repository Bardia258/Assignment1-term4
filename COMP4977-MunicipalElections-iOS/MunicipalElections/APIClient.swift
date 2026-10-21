import Foundation

final class APIClient {
    static let shared = APIClient()
    static let baseURL = URL(string: "https://municipal-elections-d8ffazd0dfevdpc6.westus3-01.azurewebsites.net/")!

    private let session: URLSession

    init(session: URLSession = .shared) {
        self.session = session
    }

    static func imageURL(for path: String?) -> URL? {
        guard let path, !path.isEmpty else { return nil }
        return URL(string: path, relativeTo: baseURL)?.absoluteURL
    }

    func fetchMunicipalities() async throws -> [Municipality] {
        let data = try await send(path: "api/municipalities")
        return try JSONDecoder().decode([Municipality].self, from: data)
    }

    func fetchCandidates(for municipalityId: Int) async throws -> [Candidate] {
        let data = try await send(path: "api/candidates", query: [
            URLQueryItem(name: "municipalityId", value: String(municipalityId))
        ])
        return try JSONDecoder().decode([Candidate].self, from: data)
    }

    func fetchAllCandidates() async throws -> [Candidate] {
        let data = try await send(path: "api/candidates")
        return try JSONDecoder().decode([Candidate].self, from: data)
    }

    func login(_ request: LoginRequest) async throws -> AuthResponse {
        let data = try await send(path: "api/auth/login", method: "POST",
                                  body: JSONEncoder().encode(request))
        return try JSONDecoder().decode(AuthResponse.self, from: data)
    }

    func register(_ request: RegisterRequest) async throws {
        _ = try await send(path: "api/auth/register", method: "POST",
                           body: JSONEncoder().encode(request))
    }

    func fetchSavedCandidates(token: String) async throws -> [SavedCandidate] {
        let data = try await send(path: "api/savedcandidates", token: token)
        return try JSONDecoder().decode([SavedCandidate].self, from: data)
    }

    func saveCandidate(id: Int, token: String) async throws -> SavedCandidate {
        let data = try await send(path: "api/savedcandidates", method: "POST",
                                  body: JSONEncoder().encode(SaveCandidateRequest(candidateId: id)), token: token)
        return try JSONDecoder().decode(SavedCandidate.self, from: data)
    }

    func deleteSavedCandidate(id: Int, token: String) async throws {
        _ = try await send(path: "api/savedcandidates/\(id)", method: "DELETE", token: token)
    }

    private func send(path: String, method: String = "GET", query: [URLQueryItem] = [],
                      body: Data? = nil, token: String? = nil) async throws -> Data {
        var components = URLComponents(url: Self.baseURL.appendingPathComponent(path), resolvingAgainstBaseURL: true)!
        if !query.isEmpty { components.queryItems = query }
        var request = URLRequest(url: components.url!)
        request.httpMethod = method
        request.httpBody = body
        request.timeoutInterval = 30
        request.setValue("application/json", forHTTPHeaderField: "Accept")
        if body != nil { request.setValue("application/json", forHTTPHeaderField: "Content-Type") }
        if let token { request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization") }

        let (data, response) = try await session.data(for: request)
        guard let response = response as? HTTPURLResponse else {
            throw APIError(statusCode: 0, message: "The server returned an invalid response.")
        }
        guard (200..<300).contains(response.statusCode) else {
            let problem = try? JSONDecoder().decode(APIProblem.self, from: data)
            let validation = problem?.errors?.sorted(by: { $0.key < $1.key }).flatMap { $0.value }.joined(separator: "\n")
            let fallback = response.statusCode == 401
                ? (token == nil ? "Invalid email or password." : "Please log in again to access your voting guide.")
                : "Request failed (HTTP \(response.statusCode)). Please try again."
            let message = [validation, problem?.message, problem?.detail, problem?.title]
                .compactMap { $0 }.first(where: { !$0.isEmpty }) ?? fallback
            throw APIError(statusCode: response.statusCode, message: message)
        }
        return data
    }
}

struct APIError: LocalizedError {
    let statusCode: Int
    let message: String
    var errorDescription: String? { message }
}

private struct APIProblem: Decodable {
    let message: String?
    let title: String?
    let detail: String?
    let errors: [String: [String]]?
}

private struct SaveCandidateRequest: Encodable {
    let candidateId: Int
}
