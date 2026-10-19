import Foundation

struct Municipality: Identifiable, Codable {
    let id: Int
    let name: String
    let province: String
    let electionDate: String // For now, use a simple string like "Oct 17, 2026"
    let description: String
    let logoUrl: String?
}

struct Candidate: Identifiable, Codable {
    let id: Int
    let name: String
    let bio: String
    let photoUrl: String?
    let positionTitle: String
    let municipalityId: Int
}
