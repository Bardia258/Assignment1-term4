import Foundation
import Combine

// Models for the API
struct LoginRequest: Codable {
    let email: String
    let password: String
}

struct RegisterRequest: Codable {
    let name: String
    let email: String
    let password: String
}

struct AuthResponse: Codable {
    let token: String
}

// MVVM ViewModel
class AuthViewModel: ObservableObject {
    @Published var email = ""
    @Published var password = ""
    @Published var name = ""
    
    @Published var isLoading = false
    @Published var errorMessage: String? = nil
    
    // Simple validation checks required by the assignment
    var isLoginValid: Bool {
        return !email.isEmpty && email.contains("@") && password.count >= 6
    }
    
    var isRegisterValid: Bool {
        return !name.isEmpty && !email.isEmpty && email.contains("@") && password.count >= 6
    }
    
    // Mock Login (swap this with URLSession later)
    func login(completion: @escaping (Bool) -> Void) {
        isLoading = true
        errorMessage = nil
        
        // Simulate network delay
        DispatchQueue.main.asyncAfter(deadline: .now() + 1.5) {
            self.isLoading = false
            
            // Hardcoded mock validation for now
            if self.email == "test@test.com" && self.password == "password123" {
                completion(true)
            } else {
                self.errorMessage = "Invalid email or password. (Use test@test.com / password123)"
                completion(false)
            }
        }
    }
    
    // Mock Register
    func register(completion: @escaping (Bool) -> Void) {
        isLoading = true
        errorMessage = nil
        
        DispatchQueue.main.asyncAfter(deadline: .now() + 1.5) {
            self.isLoading = false
            // Just pretend it worked for now
            completion(true)
        }
    }
}
