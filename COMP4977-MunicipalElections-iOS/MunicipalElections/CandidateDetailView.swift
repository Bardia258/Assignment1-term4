import SwiftUI

struct CandidateDetailView: View {
    let candidate: Candidate
    
    @EnvironmentObject var guideManager: VotingGuideManager
    
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 20) {
                
                // Photo Area
                if let photoUrl = candidate.photoUrl, let url = URL(string: photoUrl) {
                    AsyncImage(url: url) { image in
                        image.resizable().scaledToFill()
                    } placeholder: {
                        ProgressView()
                    }
                    .frame(height: 300)
                    .clipped()
                } else {
                    // Placeholder if no photo
                    ZStack {
                        Color.gray.opacity(0.3)
                        Image(systemName: "person.crop.circle.fill")
                            .resizable()
                            .scaledToFit()
                            .frame(width: 100)
                            .foregroundColor(.gray)
                    }
                    .frame(height: 250)
                    .cornerRadius(12)
                }
                
                // Header Info
                VStack(alignment: .leading, spacing: 8) {
                    Text(candidate.name)
                        .font(.largeTitle)
                        .fontWeight(.bold)
                    
                    Text(candidate.positionTitle)
                        .font(.title3)
                        .foregroundColor(.blue)
                }
                .padding(.horizontal)
                
                Divider()
                
                // Bio/Profile
                VStack(alignment: .leading, spacing: 10) {
                    Text("Profile")
                        .font(.headline)
                    Text(candidate.profile)
                        .font(.body)
                        .foregroundColor(.secondary)
                }
                .padding(.horizontal)
                
                Divider()
                
                // Contact Info
                VStack(alignment: .leading, spacing: 12) {
                    Text("Contact Information")
                        .font(.headline)
                    
                    if let website = candidate.website, !website.isEmpty {
                        HStack {
                            Image(systemName: "globe")
                            Text(website)
                                .foregroundColor(.blue)
                        }
                    }
                    
                    if let email = candidate.email, !email.isEmpty {
                        HStack {
                            Image(systemName: "envelope")
                            Text(email)
                                .foregroundColor(.blue)
                        }
                    }
                }
                .padding(.horizontal)
                .padding(.bottom, 30)
                
                // Add to Guide Button (We will wire this up in Step 3)
                Button(action: {
                    // Action goes here
                    guideManager.add(candidate: candidate)
                    print("Added \(candidate.name) to guide")
                }) {
                    Text(guideManager.isInGuide(candidate: candidate) ? "Added to Guide!" : "Add to My Voting Guide")
                        .font(.headline)
                        .foregroundColor(.white)
                        .frame(maxWidth: .infinity)
                        .padding()
                        .background(guideManager.isInGuide(candidate: candidate) ? Color.gray : Color.green)
                        .cornerRadius(10)
                }
                .padding(.horizontal)
                .padding(.bottom, 30)
                .disabled(guideManager.isInGuide(candidate: candidate))
            }
        }
        .navigationTitle("Candidate Profile")
        .navigationBarTitleDisplayMode(.inline)
    }
}

#Preview {
    NavigationStack {
        CandidateDetailView(candidate: Candidate(id: 1, name: "John Doe", profile: "Experienced leader with 10 years of city council experience.", photoUrl: nil, website: "johndoe.ca", email: "john@example.com", positionTitle: "Mayor", municipalityId: 1))
    }
}
