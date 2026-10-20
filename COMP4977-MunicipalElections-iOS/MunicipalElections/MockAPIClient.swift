import Foundation

class MockAPIClient {
    static let shared = MockAPIClient()
    
    // Fake delay to simulate network request
    private func simulateNetworkDelay() async throws {
        try await Task.sleep(nanoseconds: 1_000_000_000) // 1 second
    }
    
    func fetchMunicipalities() async throws -> [Municipality] {
        try await simulateNetworkDelay()
        return [
            Municipality(id: 1, name: "Pitt Meadows", province: "BC", electionDate: "Oct 17, 2026", description: "The City of Pitt Meadows.", logoUrl: nil),
            Municipality(id: 2, name: "Maple Ridge", province: "BC", electionDate: "Oct 17, 2026", description: "The City of Maple Ridge.", logoUrl: nil)
        ]
    }
    
    func fetchCandidates(for municipalityId: Int) async throws -> [Candidate] {
        try await simulateNetworkDelay()
        let allCandidates = [
            Candidate(id: 1, name: "John Doe", profile: "Experienced leader with 10 years of city council experience. Dedicated to improving public transit and local parks.", photoUrl: nil, website: "johndoe.ca", email: "john@example.com", positionTitle: "Mayor", municipalityId: 1),
            Candidate(id: 2, name: "Jane Smith", profile: "Community advocate and small business owner. I want to bring transparency to city hall and support local growth.", photoUrl: nil, website: nil, email: "jane@example.com", positionTitle: "Councilor", municipalityId: 1),
            Candidate(id: 3, name: "Bob Johnson", profile: "Fiscal responsibility is my top priority. Let's keep taxes low while maintaining essential services.", photoUrl: nil, website: "bobjohnson.ca", email: nil, positionTitle: "Mayor", municipalityId: 2)
        ]
        return allCandidates.filter { $0.municipalityId == municipalityId }
    }
    
    func fetchAllCandidates() async throws -> [Candidate] {
        try await simulateNetworkDelay()
        // Returning 6 candidates across different municipalities
        return [
            Candidate(id: 1, name: "John Doe", profile: "Experienced leader with 10 years of city council experience.", photoUrl: nil, website: "johndoe.ca", email: "john@example.com", positionTitle: "Mayor", municipalityId: 1),
            Candidate(id: 2, name: "Jane Smith", profile: "Community advocate and small business owner.", photoUrl: nil, website: nil, email: "jane@example.com", positionTitle: "Councilor", municipalityId: 1),
            Candidate(id: 3, name: "Bob Johnson", profile: "Fiscal responsibility is my top priority.", photoUrl: nil, website: "bobjohnson.ca", email: nil, positionTitle: "Mayor", municipalityId: 2),
            Candidate(id: 4, name: "Alice Brown", profile: "Passionate about environmental sustainability.", photoUrl: nil, website: nil, email: "alice@example.com", positionTitle: "Councilor", municipalityId: 2),
            Candidate(id: 5, name: "Charlie Davis", profile: "Dedicated to improving public education.", photoUrl: nil, website: nil, email: nil, positionTitle: "School Trustee", municipalityId: 3),
            Candidate(id: 6, name: "Diana Prince", profile: "Focusing on infrastructure and public safety.", photoUrl: nil, website: nil, email: "diana@example.com", positionTitle: "Mayor", municipalityId: 3)
        ]
    }
}
