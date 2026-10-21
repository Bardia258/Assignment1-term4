//
//  MunicipalElectionsApp.swift
//  MunicipalElections
//
//  Created by Omar Mohamed on 2026-10-17.
//

import SwiftUI

@main
struct MunicipalElectionsApp: App {
    @StateObject private var guideManager = VotingGuideManager()

    init() {
        // Existing installations must sign in again with a real account.
        if UserDefaults.standard.string(forKey: "authToken")?.hasPrefix("mock-") == true {
            UserDefaults.standard.removeObject(forKey: "authToken")
            UserDefaults.standard.removeObject(forKey: "userEmail")
        }
    }
    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(guideManager)
        }
    }
}
