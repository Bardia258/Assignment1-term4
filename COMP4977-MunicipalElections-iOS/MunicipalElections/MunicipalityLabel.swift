import SwiftUI

struct MunicipalityLogo: View {
    let logoUrl: String?
    var size: CGFloat = 32

    var body: some View {
        AsyncImage(url: APIClient.imageURL(for: logoUrl)) { phase in
            if let image = phase.image {
                image.resizable().scaledToFit().padding(3)
            } else {
                Image(systemName: "building.columns")
                    .resizable()
                    .scaledToFit()
                    .padding(7)
                    .foregroundColor(.secondary)
            }
        }
        .frame(width: size, height: size)
        .background(Color(.secondarySystemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 6))
        .accessibilityHidden(true)
    }
}

struct MunicipalityLabel: View {
    let name: String
    let logoUrl: String?
    var size: CGFloat = 28

    var body: some View {
        HStack(spacing: 8) {
            MunicipalityLogo(logoUrl: logoUrl, size: size)
            Text(name)
        }
        .accessibilityElement(children: .combine)
    }
}
