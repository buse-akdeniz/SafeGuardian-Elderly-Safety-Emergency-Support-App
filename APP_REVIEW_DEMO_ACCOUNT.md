# SafeGuardian App Review - Demo Account for Expired Subscription Testing

## Overview
Apple's app review requires testing the purchase flow with an account that has an **expired subscription**. This document provides the demo account credentials.

---

## Demo Account Credentials

### For Testing Expired Subscription

**Email:** `review.elderly@safeguardian.app`  
**Password:** `Review123!`

**Account Name:** Test User  
**Subscription Status:** ⛔ EXPIRED (Süresi Doldu)

---

## What the Reviewer Will See

### 1. Login Screen
- Email: `review.elderly@safeguardian.app`
- Password: `Review123!`

### 2. After Login - Subscription Screen
The app will display:
- **Current Plan:** ⛔ SÜRESİ DOLDU (EXPIRED)
- **Status Message (TR):** "Aboneliğinizin süresi dolmuş. Erişim devam ettirmek için yenileyin."
- **Status Message (EN):** "Subscription expired. Renew to continue access."

### 3. Account/Profile Screen
- **Plan Status:** ⛔ SÜRESİ DOLDU
- **Days Left:** 0 GÜN (0 DAYS)

### 4. Purchase Flow
From either the subscription screen, reviewer can:
- ✅ Tap "💳 SUBSCRIPTION (IN-APP PURCHASE)" button
- ✅ View available subscription plans
- ✅ Complete purchase to renew subscription
- ✅ Use "Manage Subscriptions" to view current subscriptions
- ✅ Use "Restore Purchase" to verify purchase history

---

## Technical Implementation

### Backend Configuration
- **File:** `AsistanApp/Controllers/AuthController.cs`
  - Demo account is hardcoded with special token: `demo-review-elderly-expired-token`
  - Demo user ID: 999

- **File:** `AsistanApp/Controllers/CompatibilityController.cs`
  - `ResolveUserId()` method recognizes demo token
  - `GetSubscription()` endpoint returns expired state for demo account:
    ```json
    {
      "success": true,
      "plan": "premium",
      "isActive": false,
      "expiresAt": "2026-06-09T08:31:13.350327Z",
      "trialEndsAt": "2026-05-17T08:31:13.350327Z",
      "isTrialActive": false,
      "adUnlockUntil": "2026-06-15T08:31:13.350327Z",
      "isAdUnlockActive": false,
      "hasFullAccess": false,
      "requiresSubscription": true,
      "subscriptionStatus": "expired"
    }
    ```

### Frontend Configuration
- **File:** `AsistanApp/wwwroot/elderly-ui/elderly.js`
  - `updateSubscriptionScreen()` detects expired state
  - Displays "⛔ SÜRESİ DOLDU / EXPIRED" status
  - Shows renewal message
  - Purchase buttons (💳 SUBSCRIPTION, Manage Subscriptions, Restore) remain fully accessible

---

## Testing Checklist for Reviewers

The reviewer can verify:

- [ ] Demo account login is successful
- [ ] Subscription screen displays "SÜRESİ DOLDU / EXPIRED" status
- [ ] Renewal message is clear: "Renew to continue access"
- [ ] "💳 SUBSCRIPTION (IN-APP PURCHASE)" button is clickable
- [ ] In-app purchase UI launches when tapping the subscription button
- [ ] Subscription plans are displayed in App Store
- [ ] Purchase/Renewal flow completes successfully
- [ ] "Restore Purchases" option is accessible and functional

---

## Notes for App Review Team

**Dear Apple Review Team,**

This account is specifically configured for demonstrating the purchase flow recovery for an expired subscription. The subscription expired on **June 9, 2026** (7 days ago from review date).

**Purpose:** To allow your team to review how our app handles and presents renewal options for users whose subscriptions have expired, fulfilling the requirements stated in Guideline 2.1.

**Key Points:**
- Account has no active subscription (expired)
- No active trial period
- Subscription purchase/renewal flow is fully accessible
- All StoreKit2 purchase UI integrations are functional

---

## Swift/StoreKit Configuration

The app integrates with StoreKit2 for:
- Displaying available subscription products
- Handling purchase transactions
- Validating purchase receipts with App Store

No additional test/sandbox configuration is needed beyond providing this demo account.

---

**Last Updated:** June 16, 2026  
**Build:** v1.0.0 (25)  
**Review Status:** Awaiting approval with demo account provided
