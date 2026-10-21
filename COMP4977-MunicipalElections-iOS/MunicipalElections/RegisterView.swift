import SwiftUI

struct RegisterView: View {
    @StateObject private var viewModel = AuthViewModel()
    @AppStorage("authToken") private var authToken = ""
    @AppStorage("userEmail") private var userEmail = ""
    
    var body: some View {
        VStack(spacing: 20) {
            Text("Create Account")
                .font(.largeTitle)
                .fontWeight(.bold)
                .padding(.top, 40)
            
            VStack(spacing: 15) {
                TextField("Full Name", text: $viewModel.name)
                    .autocapitalization(.words)
                    .padding()
                    .background(Color(.systemGray6))
                    .cornerRadius(10)
                
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
            .disabled(viewModel.isLoading || viewModel.accountCreated)

            Text("Use at least 6 characters, including uppercase, lowercase, a number, and a symbol.")
                .font(.caption)
                .foregroundColor(.secondary)
                .padding(.horizontal)
            
            if let error = viewModel.errorMessage {
                Text(error)
                    .foregroundColor(.red)
                    .font(.caption)
            }
            
            Button(action: {
                Task {
                    if let token = await viewModel.register() {
                        userEmail = viewModel.email.trimmingCharacters(in: .whitespacesAndNewlines)
                        authToken = token
                    }
                }
            }) {
                if viewModel.isLoading {
                    ProgressView().tint(.white)
                } else {
                    Text(viewModel.accountCreated ? "Finish Signing In" : "Register")
                        .fontWeight(.bold)
                }
            }
            .frame(maxWidth: .infinity)
            .padding()
            .background(viewModel.isRegisterValid ? Color.green : Color.gray)
            .foregroundColor(.white)
            .cornerRadius(10)
            .padding(.horizontal)
            .disabled((!viewModel.accountCreated && !viewModel.isRegisterValid) || viewModel.isLoading)
            
            Spacer()
        }
        .navigationBarTitleDisplayMode(.inline)
    }
}
