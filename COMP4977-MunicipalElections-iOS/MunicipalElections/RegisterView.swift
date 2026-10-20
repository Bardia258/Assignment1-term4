import SwiftUI

struct RegisterView: View {
    @StateObject private var viewModel = AuthViewModel()
    @Environment(\.dismiss) var dismiss
    
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
                
                SecureField("Password (min 6 chars)", text: $viewModel.password)
                    .padding()
                    .background(Color(.systemGray6))
                    .cornerRadius(10)
            }
            .padding(.horizontal)
            
            if let error = viewModel.errorMessage {
                Text(error)
                    .foregroundColor(.red)
                    .font(.caption)
            }
            
            Button(action: {
                viewModel.register { success in
                    if success {
                        dismiss() // Go back to login on success
                    }
                }
            }) {
                if viewModel.isLoading {
                    ProgressView().tint(.white)
                } else {
                    Text("Register")
                        .fontWeight(.bold)
                }
            }
            .frame(maxWidth: .infinity)
            .padding()
            .background(viewModel.isRegisterValid ? Color.green : Color.gray)
            .foregroundColor(.white)
            .cornerRadius(10)
            .padding(.horizontal)
            .disabled(!viewModel.isRegisterValid || viewModel.isLoading)
            
            Spacer()
        }
        .navigationBarTitleDisplayMode(.inline)
    }
}
