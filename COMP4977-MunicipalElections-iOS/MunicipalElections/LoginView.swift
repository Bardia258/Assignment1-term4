import SwiftUI

struct LoginView: View {
    @StateObject private var viewModel = AuthViewModel()
    @AppStorage("authToken") var authToken: String = ""
    @AppStorage("userEmail") var userEmail: String = ""
    
    var body: some View {
        NavigationStack {
            VStack(spacing: 20) {
                // Header
                Image(systemName: "building.columns.fill")
                    .font(.system(size: 60))
                    .foregroundColor(.blue)
                    .padding(.top, 40)
                
                Text("Welcome Back")
                    .font(.largeTitle)
                    .fontWeight(.bold)
                
                // Form fields
                VStack(spacing: 15) {
                    TextField("Email", text: $viewModel.email)
                        .keyboardType(.emailAddress)
                        .autocapitalization(.none)
                        .padding()
                        .background(Color(.systemGray6))
                        .cornerRadius(10)
                    
                    SecureField("Password", text: $viewModel.password)
                        .padding()
                        .background(Color(.systemGray6))
                        .cornerRadius(10)
                }
                .padding(.horizontal)
                
                // Error Message
                if let error = viewModel.errorMessage {
                    Text(error)
                        .foregroundColor(.red)
                        .font(.caption)
                        .padding(.horizontal)
                }
                
                // Login Button
                Button(action: {
                    Task {
                        if let token = await viewModel.login() {
                            userEmail = viewModel.email.trimmingCharacters(in: .whitespacesAndNewlines)
                            authToken = token
                        }
                    }
                }) {
                    if viewModel.isLoading {
                        ProgressView().tint(.white)
                    } else {
                        Text("Login")
                            .fontWeight(.bold)
                    }
                }
                .frame(maxWidth: .infinity)
                .padding()
                .background(viewModel.isLoginValid ? Color.blue : Color.gray)
                .foregroundColor(.white)
                .cornerRadius(10)
                .padding(.horizontal)
                .disabled(!viewModel.isLoginValid || viewModel.isLoading)
                
                // Link to Register
                NavigationLink(destination: RegisterView()) {
                    Text("Don't have an account? Register here.")
                        .font(.footnote)
                }
                
                Spacer()
            }
            .navigationBarHidden(true)
        }
    }
}
