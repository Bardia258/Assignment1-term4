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
            Candidate(id: 1, name: "John Doe", bio: "Experienced leader.", photoUrl: nil, positionTitle: "Mayor", municipalityId: 1),
            Candidate(id: 2, name: "Jane Smith", bio: "Community advocate.", photoUrl: nil, positionTitle: "Councilor", municipalityId: 1),
            Candidate(id: 3, name: "Bob Johnson", bio: "Fiscal responsibility.", photoUrl: nil, positionTitle: "Mayor", municipalityId: 2)
        ]
        return allCandidates.filter { $0.municipalityId == municipalityId }
    }
}
