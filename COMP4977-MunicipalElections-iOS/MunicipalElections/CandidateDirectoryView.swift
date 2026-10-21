import SwiftUI

struct CandidateDirectoryView: View {
    @StateObject private var viewModel = CandidateDirectoryViewModel()
    
    var body: some View {
        NavigationStack {
            VStack(spacing: 0) {
                if let error = viewModel.municipalityErrorMessage {
                    HStack {
                        Text("Municipality filter: \(error)").foregroundColor(.red)
                        Button("Retry") { Task { await viewModel.loadMunicipalities() } }
                    }
                    .font(.caption)
                    .padding()
                }
                Group {
                    if viewModel.isLoading {
                        ProgressView("Loading Directory...")
                    } else if let error = viewModel.errorMessage {
                        VStack(spacing: 12) {
                            Text(error).foregroundColor(.red)
                            Button("Retry") { Task { await viewModel.fetchCandidates() } }
                        }
                        .padding()
                    } else {
                        List(viewModel.filteredCandidates) { candidate in
                            NavigationLink(destination: CandidateDetailView(candidate: candidate)) {
                                HStack(spacing: 15) {
                                    // Placeholder photo
                                    Image(systemName: "person.circle.fill")
                                        .resizable()
                                        .frame(width: 50, height: 50)
                                        .foregroundColor(.gray)
                                
                                    VStack(alignment: .leading, spacing: 4) {
                                        Text(candidate.name)
                                            .font(.headline)
                                        Text(candidate.positionTitle)
                                            .font(.subheadline)
                                            .foregroundColor(.blue)
                                        MunicipalityLabel(name: candidate.displayMunicipalityName, logoUrl: candidate.municipalityLogoUrl)
                                            .font(.caption)
                                            .foregroundColor(.secondary)
                                    }
                                }
                                .padding(.vertical, 4)
                            }
                        }
                        .refreshable { await viewModel.refresh() }
                        .overlay {
                            if viewModel.filteredCandidates.isEmpty {
                                Text("No candidates found.").foregroundColor(.secondary)
                            }
                        }
                    }
                }
            }
            .searchable(text: $viewModel.searchText, prompt: "Search candidates by name")
            .navigationTitle("Directory")
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) {
                    Menu {
                        Picker("Municipality", selection: $viewModel.selectedMunicipalityId) {
                            Text("All Municipalities").tag(nil as Int?)
                            ForEach(viewModel.municipalities) { municipality in
                                Text(municipality.name).tag(Optional(municipality.id))
                            }
                        }
                    } label: {
                        HStack(spacing: 4) {
                            Text(viewModel.selectedMunicipalityName).lineLimit(1)
                            Image(systemName: "chevron.down")
                        }
                        .frame(maxWidth: 180)
                    }
                    .disabled(viewModel.municipalities.isEmpty)
                    .accessibilityLabel("Filter by municipality")
                    .accessibilityValue(viewModel.selectedMunicipalityName)
                }
            }
            .task {
                await viewModel.loadMunicipalities()
            }
            .task(id: viewModel.selectedMunicipalityId) {
                await viewModel.fetchCandidates()
            }
        }
    }
}

#Preview {
    CandidateDirectoryView()
        .environmentObject(VotingGuideManager())
}
