# App Store Review Notes (SafeGuardian)

## Test Account
- Elderly Login
  - Email: review.elderly@safeguardian.app
  - Password: Review123!
- Family Login
  - Email: family1.app-review-elderly-001@vitaguard.local
  - Password: 123

## In-App Purchase / Sandbox
- Subscriptions are implemented as Apple auto-renewable subscriptions.
- Reviewers can test purchases via Apple Sandbox. No real card is required.
- In app, the subscription page contains:
  - Buy button: "Upgrade to Family Plan"
  - Restore Purchases
  - Terms & Privacy

## Cancel / Manage Subscription
- Subscription cancel/management is available through:
  - iOS Settings > Apple ID > Subscriptions
- App includes "Manage Subscriptions" shortcut and legal links.

## Emergency Feature Note
- Emergency SMS behavior in review-safe flow is simulated in test paths.
- Production emergency routing remains available in normal operation.

## Reviewer Mode / Safe SMS
- Reviewer-safe mode is enabled for test phone number `+1234567890`.
- If this number is used in emergency SMS tests, the backend simulates success and does not call the SMS provider.
- Twilio credentials are supplied only through Railway environment variables, not hardcoded in app code.
