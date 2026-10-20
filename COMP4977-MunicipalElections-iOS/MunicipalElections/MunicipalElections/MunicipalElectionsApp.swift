//
//  MunicipalElectionsApp.swift
//  MunicipalElections
//
//  Created by Omar Mohamed on 2026-10-17.
//

import SwiftUI

@main
struct MunicipalElectionsApp: App {
    //Create the manager here
    @StateObject private var guideManager = VotingGuideManager()
    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(guideManager)
        }
    }
}
