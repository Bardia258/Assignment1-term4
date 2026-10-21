import Foundation

struct Municipality: Identifiable, Codable {
    let id: Int
    let name: String
    let province: String
    let electionDate: String
    let description: String
    let logoUrl: String?

    var formattedElectionDate: String {
        // Parse the API's calendar date without a device time zone shift.
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.timeZone = TimeZone(secondsFromGMT: 0)
        formatter.dateFormat = "yyyy-MM-dd"
        guard let date = formatter.date(from: String(electionDate.prefix(10))) else { return electionDate }
        formatter.dateStyle = .medium
        formatter.timeStyle = .none
        return formatter.string(from: date)
    }
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

struct SavedCandidate: Decodable, Identifiable {
    let id: Int
    let candidateId: Int
    let candidate: Candidate
}
