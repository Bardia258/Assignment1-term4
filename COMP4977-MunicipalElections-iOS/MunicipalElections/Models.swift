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
    let profile: String     // Matches DB "Profile"
    let photoUrl: String?   // Matches DB "PhotoFileName"
    let website: String?    // Matches DB "Website"
    let email: String?      // Matches DB "Email"
    let positionTitle: String
    let municipalityId: Int
}
