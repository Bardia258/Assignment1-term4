import Foundation
import Combine

class VotingGuideManager: ObservableObject {
    // This publishes changes to any view watching it
    @Published var selectedCandidates: [Candidate] = []
    
    private let saveKey = "SavedVotingGuide"
    
    init() {
        load()
    }
    
    func add(candidate: Candidate) {
        // Check if they are already in the list to prevent duplicates
        if !selectedCandidates.contains(where: { $0.id == candidate.id }) {
            selectedCandidates.append(candidate)
            save()
        }
    }
    
    func remove(at offsets: IndexSet) {
        selectedCandidates.remove(atOffsets: offsets)
        save()
    }
    
    func isInGuide(candidate: Candidate) -> Bool {
        return selectedCandidates.contains(where: { $0.id == candidate.id })
    }
    
    // Save to UserDefaults (phone storage)
    private func save() {
        if let encoded = try? JSONEncoder().encode(selectedCandidates) {
            UserDefaults.standard.set(encoded, forKey: saveKey)
        }
    }
    
    // Load from UserDefaults
    private func load() {
        if let data = UserDefaults.standard.data(forKey: saveKey),
           let decoded = try? JSONDecoder().decode([Candidate].self, from: data) {
            selectedCandidates = decoded
        }
    }
}
