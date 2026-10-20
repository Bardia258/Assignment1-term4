import SwiftUI

struct VotingGuideView: View {
    @EnvironmentObject var guideManager: VotingGuideManager
    
    var body: some View {
        NavigationStack {
            if guideManager.selectedCandidates.isEmpty {
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
                }
                .navigationTitle("My Voting Guide")
            } else {
                List {
                    ForEach(guideManager.selectedCandidates) { candidate in
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
                        .padding(.vertical, 4)
                    }
                    .onDelete(perform: guideManager.remove) // Swipe to delete!
                }
                .navigationTitle("My Voting Guide")
                .toolbar {
                    EditButton() // Adds an Edit button to easily delete multiple
                }
            }
        }
    }
}
