import Foundation
import Combine

@MainActor
class CandidateDirectoryViewModel: ObservableObject {
    @Published var candidates: [Candidate] = []
    @Published var searchText: String = ""
    @Published var isLoading = false
    @Published var errorMessage: String? = nil
    
    // Computed property to filter candidates based on search text
    var filteredCandidates: [Candidate] {
        if searchText.isEmpty {
            return candidates
        } else {
            return candidates.filter { $0.name.localizedCaseInsensitiveContains(searchText) }
        }
    }
    
    func fetchCandidates() async {
        isLoading = true
        errorMessage = nil
        do {
            candidates = try await MockAPIClient.shared.fetchAllCandidates()
        } catch {
            errorMessage = "Failed to load candidates."
        }
        isLoading = false
    }
}
