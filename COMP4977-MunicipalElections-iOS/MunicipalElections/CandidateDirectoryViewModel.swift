import Foundation
import Combine

@MainActor
class CandidateDirectoryViewModel: ObservableObject {
    @Published private(set) var candidates: [Candidate] = []
    @Published private(set) var municipalities: [Municipality] = []
    @Published var selectedMunicipalityId: Int?
    @Published var searchText: String = ""
    @Published var isLoading = false
    @Published var errorMessage: String? = nil
    @Published private(set) var municipalityErrorMessage: String?

    private let api: APIClient
    private var currentRequestID = UUID()

    init(api: APIClient = .shared) {
        self.api = api
    }

    var selectedMunicipalityName: String {
        municipalities.first { $0.id == selectedMunicipalityId }?.name ?? "All Municipalities"
    }
    
    // Computed property to filter candidates based on search text
    var filteredCandidates: [Candidate] {
        candidates.filter { candidate in
            (selectedMunicipalityId == nil || candidate.municipalityId == selectedMunicipalityId)
                && (searchText.isEmpty || candidate.name.localizedCaseInsensitiveContains(searchText))
        }
    }

    func loadMunicipalities() async {
        municipalityErrorMessage = nil
        do {
            let loaded = try await api.fetchMunicipalities()
            try Task.checkCancellation()
            municipalities = loaded
            if let selectedMunicipalityId, !loaded.contains(where: { $0.id == selectedMunicipalityId }) {
                self.selectedMunicipalityId = nil
            }
        } catch {
            if !Task.isCancelled { municipalityErrorMessage = error.localizedDescription }
        }
    }
    
    func fetchCandidates() async {
        let requestID = UUID()
        currentRequestID = requestID
        let municipalityId = selectedMunicipalityId
        isLoading = true
        errorMessage = nil
        defer { if currentRequestID == requestID { isLoading = false } }
        do {
            let loaded: [Candidate]
            if let municipalityId {
                loaded = try await api.fetchCandidates(for: municipalityId)
            } else {
                loaded = try await api.fetchAllCandidates()
            }
            try Task.checkCancellation()
            guard currentRequestID == requestID, selectedMunicipalityId == municipalityId else { return }
            candidates = loaded
        } catch {
            if !Task.isCancelled && currentRequestID == requestID && selectedMunicipalityId == municipalityId {
                errorMessage = error.localizedDescription
            }
        }
    }

    func refresh() async {
        await loadMunicipalities()
        await fetchCandidates()
    }
}
