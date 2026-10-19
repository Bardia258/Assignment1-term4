import SwiftUI

struct ContentView: View {
    var body: some View {
        TabView {
            MunicipalityListView()
                .tabItem {
                    Label("Municipalities", systemImage: "building.columns")
                }
            
            Text("Voting Guide Coming Soon")
                .tabItem {
                    Label("My Guide", systemImage: "checklist")
                }
            
            Text("Login Coming Soon")
                .tabItem {
                    Label("Account", systemImage: "person.circle")
                }
        }
    }
}

struct MunicipalityListView: View {
    @State private var municipalities: [Municipality] = []
    @State private var isLoading = false
    
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
                }
            }
            .task {
                // This runs when the view loads
                isLoading = true
                do {
                    municipalities = try await MockAPIClient.shared.fetchMunicipalities()
                } catch {
                    print("Error: \(error)")
                }
                isLoading = false
            }
        }
    }
}

struct MunicipalityDetailView: View {
    let municipality: Municipality
    @State private var candidates: [Candidate] = []
    @State private var isLoading = false
    
    var body: some View {
        List {
            Section("About") {
                Text(municipality.description)
                Text("Election: \(municipality.electionDate)")
                    .font(.footnote)
                    .foregroundColor(.red)
            }
            
            Section("Candidates") {
                if isLoading {
                    ProgressView()
                } else if candidates.isEmpty {
                    Text("No candidates yet.")
                        .foregroundColor(.secondary)
                } else {
                    ForEach(candidates) { candidate in
                        VStack(alignment: .leading) {
                            Text(candidate.name)
                                .font(.headline)
                            Text(candidate.positionTitle)
                                .font(.subheadline)
                                .foregroundColor(.blue)
                            Text(candidate.bio)
                                .font(.caption)
                                .foregroundColor(.secondary)
                        }
                        .padding(.vertical, 4)
                    }
                }
            }
        }
        .navigationTitle(municipality.name)
        .task {
            isLoading = true
            do {
                candidates = try await MockAPIClient.shared.fetchCandidates(for: municipality.id)
            } catch {
                print("Error: \(error)")
            }
            isLoading = false
        }
    }
}

#Preview {
    ContentView()
}
