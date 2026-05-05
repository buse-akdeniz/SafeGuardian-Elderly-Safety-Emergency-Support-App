import Foundation
import Capacitor
import StoreKit

@objc(StoreKit2Plugin)
public class StoreKit2Plugin: CAPPlugin, CAPBridgedPlugin {
    public let identifier = "StoreKit2Plugin"
    public let jsName = "StoreKit2"
    public let pluginMethods: [CAPPluginMethod] = [
        CAPPluginMethod(name: "isAvailable", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "getProducts", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "purchaseProduct", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "restorePurchases", returnType: CAPPluginReturnPromise)
    ]

    private let implementation = StoreKit2Bridge()

    @objc func isAvailable(_ call: CAPPluginCall) {
        if #available(iOS 15.0, *) {
            call.resolve(["available": true])
        } else {
            call.resolve(["available": false])
        }
    }

    @objc func getProducts(_ call: CAPPluginCall) {
        guard #available(iOS 15.0, *) else {
            call.unavailable("StoreKit 2 requires iOS 15 or later.")
            return
        }

        guard let productIds = call.options["productIds"] as? [String], !productIds.isEmpty else {
            call.reject("productIds array is required")
            return
        }

        Task {
            do {
                let products = try await implementation.getProducts(productIds: productIds)
                call.resolve(["products": products])
            } catch {
                call.reject(error.localizedDescription, nil, error)
            }
        }
    }

    @objc func purchaseProduct(_ call: CAPPluginCall) {
        guard #available(iOS 15.0, *) else {
            call.unavailable("StoreKit 2 requires iOS 15 or later.")
            return
        }

        guard let productId = call.getString("productId"), !productId.isEmpty else {
            call.reject("productId is required")
            return
        }

        Task {
            do {
                let result = try await implementation.purchaseProduct(productId: productId)
                call.resolve(result)
            } catch {
                call.reject(error.localizedDescription, nil, error)
            }
        }
    }

    @objc func restorePurchases(_ call: CAPPluginCall) {
        guard #available(iOS 15.0, *) else {
            call.unavailable("StoreKit 2 requires iOS 15 or later.")
            return
        }

        Task {
            do {
                let result = try await implementation.restorePurchases()
                call.resolve(result)
            } catch {
                call.reject(error.localizedDescription, nil, error)
            }
        }
    }
}

@available(iOS 15.0, *)
private final class StoreKit2Bridge: NSObject {
    func getProducts(productIds: [String]) async throws -> [[String: Any]] {
        let products = try await Product.products(for: productIds)
        return products.map { product in
            [
                "id": product.id,
                "displayName": product.displayName,
                "description": product.description,
                "displayPrice": product.displayPrice,
                "type": String(describing: product.type)
            ]
        }
    }

    func purchaseProduct(productId: String) async throws -> [String: Any] {
        let products = try await Product.products(for: [productId])
        guard let product = products.first else {
            throw NSError(domain: "StoreKit2Plugin", code: 404, userInfo: [NSLocalizedDescriptionKey: "Product not found in App Store Connect."])
        }

        let result = try await product.purchase()
        switch result {
        case .success(let verification):
            let transaction = try verify(verification)
            await transaction.finish()
            return [
                "success": true,
                "cancelled": false,
                "pending": false,
                "productId": transaction.productID,
                "transactionId": String(transaction.id),
                "expirationDate": transaction.expirationDate?.ISO8601Format() ?? ""
            ]
        case .userCancelled:
            return [
                "success": false,
                "cancelled": true,
                "pending": false
            ]
        case .pending:
            return [
                "success": false,
                "cancelled": false,
                "pending": true
            ]
        @unknown default:
            return [
                "success": false,
                "cancelled": false,
                "pending": false
            ]
        }
    }

    func restorePurchases() async throws -> [String: Any] {
        try await AppStore.sync()
        var purchases: [[String: Any]] = []

        for await entitlement in Transaction.currentEntitlements {
            let transaction = try verify(entitlement)
            purchases.append([
                "productId": transaction.productID,
                "transactionId": String(transaction.id),
                "expirationDate": transaction.expirationDate?.ISO8601Format() ?? ""
            ])
        }

        return [
            "success": true,
            "purchases": purchases
        ]
    }

    private func verify<T>(_ result: VerificationResult<T>) throws -> T {
        switch result {
        case .verified(let value):
            return value
        case .unverified:
            throw NSError(domain: "StoreKit2Plugin", code: 401, userInfo: [NSLocalizedDescriptionKey: "StoreKit transaction could not be verified."])
        }
    }
}
