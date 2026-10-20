import SwiftUI

struct CandidateDirectoryView: View {
    @StateObject private var viewModel = CandidateDirectoryViewModel()
    
    var body: some View {
        NavigationStack {
            Group {
                if viewModel.isLoading {
                    ProgressView("Loading Directory...")
                } else if let error = viewModel.errorMessage {
                    Text(error).foregroundColor(.red)
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
                                    Text("Municipality ID: \(candidate.municipalityId)")
                                        .font(.caption)
                                        .foregroundColor(.secondary)
                                }
                            }
                            .padding(.vertical, 4)
                        }
                    }
                    // Native iOS Search Bar
                    .searchable(text: $viewModel.searchText, prompt: "Search candidates by name")
                }
            }
            .navigationTitle("Directory")
            .task {
                // Only fetch if we don't have data yet
                if viewModel.candidates.isEmpty {
                    await viewModel.fetchCandidates()
                }
            }
        }
    }
}

#Preview {
    CandidateDirectoryView()
}
