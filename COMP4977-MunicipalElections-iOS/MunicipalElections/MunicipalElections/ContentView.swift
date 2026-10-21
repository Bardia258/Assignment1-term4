import SwiftUI

struct ContentView: View {
    
    @AppStorage("authToken") var authToken: String = ""
    @EnvironmentObject var guideManager: VotingGuideManager
    
    var body: some View {
        TabView {
            MunicipalityListView()
                .tabItem {
                    Label("Municipalities", systemImage: "building.columns")
                }
            
            CandidateDirectoryView()
                .tabItem {
                    Label("Directory", systemImage: "magnifyingglass")
                }
                    
            // Here is Voting Guide Tab
            VotingGuideView()
                .tabItem {
                    Label("My Guide", systemImage: "checklist")
                }
            
            // Here is Account Tab
            Group {
                if authToken.isEmpty {
                    LoginView()
                } else {
                    ProfileView()
                }
            }
            .tabItem{
                Label("Account", systemImage: "person.circle")
            }
        }
        .task(id: authToken) {
            await guideManager.load(token: authToken)
        }
    }
}

struct MunicipalityListView: View {
    @State private var municipalities: [Municipality] = []
    @State private var isLoading = false
    @State private var errorMessage: String?
    
    var body: some View {
        NavigationStack {
            List(municipalities) { muni in
                NavigationLink(destination: MunicipalityDetailView(municipality: muni)) {
                    VStack(alignment: .leading) {
                        Text(muni.name)
                            .font(.headline)
                        Text(muni.province)
                            .font(.subheadline)
                            .foregroundColor(.secondary)
                    }
                    .padding(.vertical, 4)
                }
            }
            .navigationTitle("Municipalities")
            .overlay {
                if isLoading {
                    ProgressView("Loading...")
                } else if let error = errorMessage {
                    VStack(spacing: 12) {
                        Text(error).foregroundColor(.red)
                        Button("Retry") { Task { await loadMunicipalities() } }
                    }
                    .padding()
                } else if municipalities.isEmpty {
                    Text("No municipalities available.").foregroundColor(.secondary)
                }
            }
            .refreshable { await loadMunicipalities() }
            .task {
                await loadMunicipalities()
            }
        }
    }

    @MainActor
    private func loadMunicipalities() async {
        isLoading = true
        errorMessage = nil
        defer { isLoading = false }
        do {
            municipalities = try await APIClient.shared.fetchMunicipalities()
        } catch {
            if !Task.isCancelled { errorMessage = error.localizedDescription }
        }
    }
}

struct MunicipalityDetailView: View {
    let municipality: Municipality
    @State private var candidates: [Candidate] = []
    @State private var isLoading = false
    @State private var errorMessage: String?
    
    var body: some View {
        List {
            Section("About") {
                Text(municipality.description)
                Text("Election: \(municipality.formattedElectionDate)")
                    .font(.footnote)
                    .foregroundColor(.red)
            }
            
            Section("Candidates") {
                if isLoading {
                    ProgressView()
                } else if let error = errorMessage {
                    Text(error).foregroundColor(.red)
                    Button("Retry") { Task { await loadCandidates() } }
                } else if candidates.isEmpty {
                    Text("No candidates yet.")
                        .foregroundColor(.secondary)
                } else {
                    ForEach(candidates) { candidate in
                        NavigationLink(destination: CandidateDetailView(candidate: candidate)) {
                            VStack(alignment: .leading, spacing: 4) {
                                Text(candidate.name)
                                    .font(.headline)
                                Text(candidate.positionTitle)
                                    .font(.subheadline)
                                    .foregroundColor(.blue)
                                Text(candidate.profile)
                                    .font(.caption)
                                    .foregroundColor(.secondary)
                                    .lineLimit(2)
                            }
                            .padding(.vertical, 4)
                        }
                    }
                }
            }
        }
        .navigationTitle(municipality.name)
        .refreshable { await loadCandidates() }
        .task {
            await loadCandidates()
        }
    }

    @MainActor
    private func loadCandidates() async {
        isLoading = true
        errorMessage = nil
        defer { isLoading = false }
        do {
            candidates = try await APIClient.shared.fetchCandidates(for: municipality.id)
        } catch {
            if !Task.isCancelled { errorMessage = error.localizedDescription }
        }
    }
}


#Preview {
    ContentView()
        .environmentObject(VotingGuideManager())
}
