import Foundation
import XCTest
@testable import MunicipalElections

final class MunicipalElectionsTests: XCTestCase {

    private var session: URLSession!
    private var client: APIClient!

    private static let candidateJSON = """
    {"id":42,"name":"Alex Lee","profile":"Community advocate","photoUrl":"/uploads/candidates/photo.jpg",
     "website":null,"email":"alex@example.com","positionTitle":"Mayor","municipalityId":7,
     "municipalityName":"Pitt Meadows","dateCreated":"2026-10-17T00:00:00","positionId":3,
     "municipality":{"id":7,"name":"Pitt Meadows","province":"BC","electionDate":"2026-10-17T00:00:00",
                     "description":"City of Pitt Meadows","logoUrl":"/uploads/logos/pitt-meadows.png"}}
    """

    override func setUpWithError() throws {
        let configuration = URLSessionConfiguration.ephemeral
        configuration.protocolClasses = [StubURLProtocol.self]
        session = URLSession(configuration: configuration)
        client = APIClient(session: session)
    }

    override func tearDownWithError() throws {
        session.invalidateAndCancel()
        StubURLProtocol.handler = nil
        client = nil
        session = nil
    }

    func testDirectoryDecodesBackendJSONAndRelativeImages() async throws {
        StubURLProtocol.handler = { request in
            XCTAssertEqual(request.url?.host, APIClient.baseURL.host)
            XCTAssertNil(request.value(forHTTPHeaderField: "Authorization"))
            switch request.url?.path {
            case "/api/municipalities":
                return (200, Data("""
                [{"id":7,"name":"Pitt Meadows","province":"BC","electionDate":"2026-10-17T00:00:00",
                  "description":"City of Pitt Meadows","logoUrl":null}]
                """.utf8))
            case "/api/candidates":
                return (200, Data("[\(Self.candidateJSON)]".utf8))
            default:
                XCTFail("Unexpected route: \(request.url?.path ?? "")")
                return (404, Data())
            }
        }
        let municipalities = try await client.fetchMunicipalities()
        XCTAssertEqual(municipalities.first?.electionDate, "2026-10-17T00:00:00")
        XCTAssertEqual(municipalities.first?.formattedElectionDate, "Oct 17, 2026")
        let candidates = try await client.fetchAllCandidates()
        XCTAssertEqual(candidates.first?.id, 42)
        XCTAssertEqual(candidates.first?.displayMunicipalityName, "Pitt Meadows")
        XCTAssertEqual(APIClient.imageURL(for: candidates.first?.municipalityLogoUrl)?.absoluteString,
                       APIClient.baseURL.absoluteString + "uploads/logos/pitt-meadows.png")
        XCTAssertEqual(APIClient.imageURL(for: candidates.first?.photoUrl)?.absoluteString,
                       APIClient.baseURL.absoluteString + "uploads/candidates/photo.jpg")
        XCTAssertNil(APIClient.imageURL(for: nil))
        XCTAssertEqual(APIClient.imageURL(for: "https://example.com/photo.jpg")?.host, "example.com")
    }

    func testCandidatesAreFilteredUsingMunicipalityQueryParameter() async throws {
        StubURLProtocol.handler = { request in
            XCTAssertEqual(request.url?.path, "/api/candidates")
            let query = URLComponents(url: try XCTUnwrap(request.url), resolvingAgainstBaseURL: true)?.queryItems
            XCTAssertEqual(query?.first(where: { $0.name == "municipalityId" })?.value, "7")
            return (200, Data("[]".utf8))
        }
        let candidates = try await client.fetchCandidates(for: 7)
        XCTAssertTrue(candidates.isEmpty)
    }

    @MainActor
    func testDirectoryCombinesMunicipalitySelectionAndNameSearch() async throws {
        let otherCandidate = Self.candidateJSON
            .replacingOccurrences(of: "\"id\":42", with: "\"id\":43")
            .replacingOccurrences(of: "\"id\":7", with: "\"id\":8")
            .replacingOccurrences(of: "\"municipalityId\":7", with: "\"municipalityId\":8")
            .replacingOccurrences(of: "Pitt Meadows", with: "Maple Ridge")
            .replacingOccurrences(of: "Alex Lee", with: "Sam Chen")
        StubURLProtocol.handler = { request in
            if request.url?.path == "/api/municipalities" {
                return (200, Data("""
                [{"id":7,"name":"Pitt Meadows","province":"BC","electionDate":"2026-10-17T00:00:00","description":"","logoUrl":null},
                 {"id":8,"name":"Maple Ridge","province":"BC","electionDate":"2026-10-17T00:00:00","description":"","logoUrl":null}]
                """.utf8))
            }
            let query = URLComponents(url: try XCTUnwrap(request.url), resolvingAgainstBaseURL: true)?.queryItems
            switch query?.first(where: { $0.name == "municipalityId" })?.value {
            case "7": return (200, Data("[\(Self.candidateJSON)]".utf8))
            case "8": return (200, Data("[\(otherCandidate)]".utf8))
            default: return (200, Data("[\(Self.candidateJSON),\(otherCandidate)]".utf8))
            }
        }
        let viewModel = CandidateDirectoryViewModel(api: client)
        await viewModel.loadMunicipalities()
        await viewModel.fetchCandidates()
        XCTAssertEqual(viewModel.municipalities.count, 2)
        XCTAssertEqual(viewModel.filteredCandidates.count, 2)

        viewModel.selectedMunicipalityId = 7
        await viewModel.fetchCandidates()
        XCTAssertEqual(viewModel.selectedMunicipalityName, "Pitt Meadows")
        XCTAssertEqual(viewModel.filteredCandidates.map(\.id), [42])
        viewModel.searchText = "alex"
        XCTAssertEqual(viewModel.filteredCandidates.count, 1)

        viewModel.selectedMunicipalityId = 8
        await viewModel.fetchCandidates()
        XCTAssertTrue(viewModel.filteredCandidates.isEmpty)
        viewModel.searchText = ""
        XCTAssertEqual(viewModel.filteredCandidates.map(\.id), [43])

        viewModel.selectedMunicipalityId = nil
        await viewModel.fetchCandidates()
        XCTAssertEqual(viewModel.selectedMunicipalityName, "All Municipalities")
        XCTAssertEqual(viewModel.filteredCandidates.count, 2)
    }

    func testCandidateWithoutNestedMunicipalityKeepsNameAndHasNoLogo() throws {
        var json = try XCTUnwrap(JSONSerialization.jsonObject(with: Data(Self.candidateJSON.utf8)) as? [String: Any])
        json.removeValue(forKey: "municipality")
        let candidate = try JSONDecoder().decode(Candidate.self, from: JSONSerialization.data(withJSONObject: json))
        XCTAssertEqual(candidate.displayMunicipalityName, "Pitt Meadows")
        XCTAssertNil(candidate.municipalityLogoUrl)
    }

    func testLoginSendsUsernameAndReturnsServerToken() async throws {
        StubURLProtocol.handler = { request in
            XCTAssertEqual(request.url?.path, "/api/auth/login")
            XCTAssertEqual(request.httpMethod, "POST")
            XCTAssertEqual(request.value(forHTTPHeaderField: "Content-Type"), "application/json")
            let body = try Self.jsonBody(request)
            XCTAssertEqual(body["username"] as? String, "voter@example.com")
            XCTAssertEqual(body["password"] as? String, "Password1!")
            XCTAssertNil(body["email"])
            return (200, Data(#"{"token":"server-issued-token"}"#.utf8))
        }
        let response = try await client.login(LoginRequest(username: "voter@example.com", password: "Password1!"))
        XCTAssertEqual(response.token, "server-issued-token")
    }

    func testRegistrationAcceptsCreatedResponseWithoutToken() async throws {
        StubURLProtocol.handler = { request in
            XCTAssertEqual(request.url?.path, "/api/auth/register")
            XCTAssertEqual(request.httpMethod, "POST")
            let body = try Self.jsonBody(request)
            XCTAssertEqual(body["username"] as? String, "voter@example.com")
            XCTAssertEqual(body["email"] as? String, "voter@example.com")
            XCTAssertEqual(body["fullName"] as? String, "Alex Lee")
            return (201, Data(#"{"id":"user-id","username":"voter@example.com"}"#.utf8))
        }
        try await client.register(RegisterRequest(username: "voter@example.com", email: "voter@example.com",
                                                   password: "Password1!", fullName: "Alex Lee"))
    }

    @MainActor
    func testRegistrationAutomaticallyLogsInWithTheSameCredentials() async throws {
        var routes: [String] = []
        StubURLProtocol.handler = { request in
            let path = try XCTUnwrap(request.url?.path)
            routes.append(path)
            let body = try Self.jsonBody(request)
            XCTAssertEqual(body["username"] as? String, "new-voter@example.com")
            XCTAssertEqual(body["password"] as? String, "Password1!")
            if path == "/api/auth/register" {
                XCTAssertEqual(body["fullName"] as? String, "New Voter")
                return (201, Data(#"{"id":"new-user","username":"new-voter@example.com"}"#.utf8))
            }
            XCTAssertEqual(path, "/api/auth/login")
            return (200, Data(#"{"token":"new-account-token"}"#.utf8))
        }
        let viewModel = AuthViewModel(api: client)
        viewModel.name = "New Voter"
        viewModel.email = "  new-voter@example.com  "
        viewModel.password = "Password1!"
        let token = await viewModel.register()
        XCTAssertEqual(token, "new-account-token")
        XCTAssertEqual(routes, ["/api/auth/register", "/api/auth/login"])
        XCTAssertTrue(viewModel.accountCreated)
        XCTAssertNil(viewModel.errorMessage)
        XCTAssertFalse(viewModel.isLoading)
    }

    @MainActor
    func testFailedAutomaticLoginCanRetryWithoutRegisteringAgain() async throws {
        var registrations = 0
        var logins = 0
        StubURLProtocol.handler = { request in
            if request.url?.path == "/api/auth/register" {
                registrations += 1
                return (201, Data(#"{"id":"new-user","username":"voter@example.com"}"#.utf8))
            }
            XCTAssertEqual(request.url?.path, "/api/auth/login")
            let body = try Self.jsonBody(request)
            XCTAssertEqual(body["username"] as? String, "voter@example.com")
            XCTAssertEqual(body["password"] as? String, "Password1!")
            logins += 1
            if logins == 1 {
                return (503, Data(#"{"message":"Temporary sign-in issue."}"#.utf8))
            }
            return (200, Data(#"{"token":"retry-token"}"#.utf8))
        }
        let viewModel = AuthViewModel(api: client)
        viewModel.name = "Voter"
        viewModel.email = "voter@example.com"
        viewModel.password = "Password1!"
        let firstToken = await viewModel.register()
        XCTAssertNil(firstToken)
        XCTAssertTrue(viewModel.accountCreated)
        XCTAssertTrue(viewModel.errorMessage?.contains("Your account was created") == true)
        let retryToken = await viewModel.register()
        XCTAssertEqual(retryToken, "retry-token")
        XCTAssertEqual(registrations, 1)
        XCTAssertEqual(logins, 2)
    }

    @MainActor
    func testFailedRegistrationDoesNotAttemptLogin() async throws {
        StubURLProtocol.handler = { request in
            XCTAssertEqual(request.url?.path, "/api/auth/register")
            return (400, Data(#"{"errors":{"Email":["This email is already registered."]}}"#.utf8))
        }
        let viewModel = AuthViewModel(api: client)
        viewModel.name = "Voter"
        viewModel.email = "voter@example.com"
        viewModel.password = "Password1!"
        let token = await viewModel.register()
        XCTAssertNil(token)
        XCTAssertFalse(viewModel.accountCreated)
        XCTAssertEqual(viewModel.errorMessage, "This email is already registered.")
        XCTAssertFalse(viewModel.isLoading)
    }

    func testSavedCandidatesUseBearerTokenAndDeleteSavedRecordID() async throws {
        let savedJSON = "{\"id\":91,\"candidateId\":42,\"dateSaved\":\"2026-10-21T12:00:00Z\",\"candidate\":\(Self.candidateJSON)}"
        StubURLProtocol.handler = { request in
            XCTAssertEqual(request.value(forHTTPHeaderField: "Authorization"), "Bearer voter-token")
            switch request.httpMethod {
            case "GET":
                XCTAssertEqual(request.url?.path, "/api/savedcandidates")
                return (200, Data("[\(savedJSON)]".utf8))
            case "POST":
                XCTAssertEqual(request.url?.path, "/api/savedcandidates")
                XCTAssertEqual(try Self.jsonBody(request)["candidateId"] as? Int, 42)
                return (201, Data(savedJSON.utf8))
            case "DELETE":
                XCTAssertEqual(request.url?.path, "/api/savedcandidates/91")
                return (204, Data())
            default:
                XCTFail("Unexpected method")
                return (405, Data())
            }
        }
        let saved = try await client.fetchSavedCandidates(token: "voter-token")
        XCTAssertEqual(saved.first?.candidate.id, 42)
        let created = try await client.saveCandidate(id: 42, token: "voter-token")
        XCTAssertEqual(created.id, 91)
        try await client.deleteSavedCandidate(id: created.id, token: "voter-token")
    }

    func testValidationErrorsAreShownFromBackend() async throws {
        StubURLProtocol.handler = { _ in
            (400, Data(#"{"title":"Validation failed","errors":{"Password":["Passwords must have at least one digit."]}}"#.utf8))
        }
        do {
            try await client.register(RegisterRequest(username: "voter@example.com", email: "voter@example.com",
                                                       password: "password", fullName: "Alex Lee"))
            XCTFail("Expected the validation request to fail")
        } catch let error as APIError {
            XCTAssertEqual(error.statusCode, 400)
            XCTAssertEqual(error.localizedDescription, "Passwords must have at least one digit.")
        }
    }

    func testUnauthorizedResponseIsNotDecodedAsSuccess() async throws {
        StubURLProtocol.handler = { _ in (401, Data()) }
        do {
            _ = try await client.fetchSavedCandidates(token: "expired-token")
            XCTFail("Expected expired authentication to fail")
        } catch let error as APIError {
            XCTAssertEqual(error.statusCode, 401)
            XCTAssertTrue(error.localizedDescription.contains("log in again"))
        }
    }

    private static func jsonBody(_ request: URLRequest) throws -> [String: Any] {
        var data = request.httpBody ?? Data()
        if data.isEmpty, let stream = request.httpBodyStream {
            stream.open()
            defer { stream.close() }
            var buffer = [UInt8](repeating: 0, count: 1024)
            while stream.hasBytesAvailable {
                let count = stream.read(&buffer, maxLength: buffer.count)
                guard count > 0 else { break }
                data.append(contentsOf: buffer.prefix(count))
            }
        }
        return try XCTUnwrap(JSONSerialization.jsonObject(with: data) as? [String: Any])
    }

}

private final class StubURLProtocol: URLProtocol {
    static var handler: ((URLRequest) throws -> (Int, Data))?

    override class func canInit(with request: URLRequest) -> Bool { true }
    override class func canonicalRequest(for request: URLRequest) -> URLRequest { request }

    override func startLoading() {
        do {
            let handler = try XCTUnwrap(Self.handler)
            let (status, data) = try handler(request)
            let response = try XCTUnwrap(HTTPURLResponse(url: try XCTUnwrap(request.url), statusCode: status,
                httpVersion: nil, headerFields: ["Content-Type": "application/json"]))
            client?.urlProtocol(self, didReceive: response, cacheStoragePolicy: .notAllowed)
            client?.urlProtocol(self, didLoad: data)
            client?.urlProtocolDidFinishLoading(self)
        } catch { client?.urlProtocol(self, didFailWithError: error) }
    }

    override func stopLoading() { }
}
