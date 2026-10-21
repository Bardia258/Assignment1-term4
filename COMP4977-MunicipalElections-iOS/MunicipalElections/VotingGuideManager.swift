import Foundation
import Combine

@MainActor
class VotingGuideManager: ObservableObject {
    @Published private var savedCandidates: [SavedCandidate] = []
    @Published private(set) var isLoading = false
    @Published private(set) var errorMessage: String?

    private var activeToken = ""

    var selectedCandidates: [Candidate] { savedCandidates.map(\.candidate) }

    func load(token: String) async {
        if activeToken != token {
            savedCandidates = []
            activeToken = token
            isLoading = false
        }
        errorMessage = nil
        guard !token.isEmpty, !isLoading else { return }
        isLoading = true
        defer { if activeToken == token { isLoading = false } }
        do {
            let saved = try await APIClient.shared.fetchSavedCandidates(token: token)
            guard isCurrent(token) else { return }
            savedCandidates = saved
        } catch {
            if !Task.isCancelled { handle(error, token: token) }
        }
    }

    func add(candidate: Candidate, token: String) async {
        guard !token.isEmpty, isCurrent(token), !isLoading, !isInGuide(candidate: candidate) else { return }
        isLoading = true
        errorMessage = nil
        defer { if activeToken == token { isLoading = false } }
        do {
            let saved = try await APIClient.shared.saveCandidate(id: candidate.id, token: token)
            guard isCurrent(token) else { return }
            savedCandidates.append(saved)
        } catch let error as APIError where error.statusCode == 409 {
            // Another device may have already saved this candidate.
            do {
                let saved = try await APIClient.shared.fetchSavedCandidates(token: token)
                if isCurrent(token) { savedCandidates = saved }
            } catch { handle(error, token: token) }
        } catch { handle(error, token: token) }
    }

    func remove(candidateIDs: [Int], token: String) async {
        guard !token.isEmpty, isCurrent(token), !isLoading else { return }
        let records = savedCandidates.filter { candidateIDs.contains($0.candidateId) }
        isLoading = true
        errorMessage = nil
        defer { if activeToken == token { isLoading = false } }
        do {
            for saved in records {
                guard isCurrent(token) else { return }
                // DELETE expects the saved record ID, not the candidate ID.
                try await APIClient.shared.deleteSavedCandidate(id: saved.id, token: token)
                guard isCurrent(token) else { return }
                savedCandidates.removeAll { $0.id == saved.id }
            }
        } catch { handle(error, token: token) }
    }

    func isInGuide(candidate: Candidate) -> Bool {
        savedCandidates.contains { $0.candidateId == candidate.id }
    }

    private func isCurrent(_ token: String) -> Bool {
        activeToken == token && UserDefaults.standard.string(forKey: "authToken") == token
    }

    private func handle(_ error: Error, token: String) {
        guard isCurrent(token) else { return }
        errorMessage = error.localizedDescription
        if let error = error as? APIError, error.statusCode == 401 {
            savedCandidates = []
            UserDefaults.standard.removeObject(forKey: "authToken")
            UserDefaults.standard.removeObject(forKey: "userEmail")
        }
    }
}
