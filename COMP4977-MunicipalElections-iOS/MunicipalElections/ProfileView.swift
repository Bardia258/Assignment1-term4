import SwiftUI

struct ProfileView: View {
    @AppStorage("authToken") var authToken: String = ""
    @AppStorage("userEmail") var userEmail: String = ""
    
    var body: some View {
        NavigationStack {
            VStack(spacing: 30) {
                Image(systemName: "person.crop.circle.fill")
                    .font(.system(size: 80))
                    .foregroundColor(.blue)
                
                Text(userEmail)
                    .font(.title2)
                    .fontWeight(.bold)
                
                Text("Authenticated User")
                    .foregroundColor(.green)
                    .font(.subheadline)
                
                Spacer()
                
                Button(action: {
                    // Logout
                    authToken = ""
                    userEmail = ""
                }) {
                    Text("Log Out")
                        .fontWeight(.bold)
                        .frame(maxWidth: .infinity)
                        .padding()
                        .background(Color.red)
                        .foregroundColor(.white)
                        .cornerRadius(10)
                }
                .padding(.horizontal)
                .padding(.bottom, 40)
            }
            .navigationTitle("My Profile")
        }
    }
}
