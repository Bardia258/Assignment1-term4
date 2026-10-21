import SwiftUI

struct VotingGuideView: View {
    @EnvironmentObject var guideManager: VotingGuideManager
    @AppStorage("authToken") private var authToken = ""
    
    var body: some View {
        NavigationStack {
            Group {
                if authToken.isEmpty {
                    Text("Log in from the Account tab to view and save your voting guide.")
                        .foregroundColor(.secondary)
                        .multilineTextAlignment(.center)
                        .padding()
                } else if guideManager.isLoading && guideManager.selectedCandidates.isEmpty {
                    ProgressView("Loading Guide...")
                } else if let error = guideManager.errorMessage {
                    VStack(spacing: 12) {
                        Text(error).foregroundColor(.red)
                        Button("Retry") { Task { await guideManager.load(token: authToken) } }
                    }
                    .padding()
                } else if guideManager.selectedCandidates.isEmpty {
                    VStack(spacing: 20) {
                        Image(systemName: "checklist")
                            .font(.system(size: 60))
                            .foregroundColor(.gray)
                        Text("Your guide is empty.")
                            .font(.headline)
                        Text("Browse municipalities and add candidates to build your personalized voting guide.")
                            .font(.subheadline)
                            .foregroundColor(.secondary)
                            .multilineTextAlignment(.center)
                            .padding(.horizontal)
                        Button("Refresh") { Task { await guideManager.load(token: authToken) } }
                    }
                } else {
                    List {
                        ForEach(guideManager.selectedCandidates) { candidate in
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
                            .padding(.vertical, 4)
                        }
                        .onDelete { offsets in
                            let ids = offsets.map { guideManager.selectedCandidates[$0].id }
                            Task { await guideManager.remove(candidateIDs: ids, token: authToken) }
                        }
                        .deleteDisabled(guideManager.isLoading)
                    }
                    .refreshable { await guideManager.load(token: authToken) }
                    .toolbar {
                        EditButton().disabled(guideManager.isLoading)
                    }
                }
            }
            .navigationTitle("My Voting Guide")
        }
    }
}
