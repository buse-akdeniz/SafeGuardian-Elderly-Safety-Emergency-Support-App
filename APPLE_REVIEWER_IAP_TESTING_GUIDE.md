# In-App Purchase Debugging Guide for Apple Reviewers

## Quick Start: Testing IAP in This App

This guide explains how to test the In-App Purchase functionality in **Safeguardian** during the review process.

---

## Device & Environment Setup

### What Was Provided by Developers
- **Device**: iPad Air 11-inch (M3)
- **OS**: iPadOS 26.5 or later
- **Build**: Version 1.0.0 (build 12)
- **Test Account Type**: Sandbox tester account (provided separately)

### Prerequisite
- Ensure you're signed into **Sandbox Tester account**, NOT your personal Apple ID
- If prompted "Your account is not authorized to purchase", you're using the wrong account

---

## Step 1: Launch App and Navigate to Subscription

1. **Install and launch** the Safeguardian app on iPad
2. **Sign in** with the provided Sandbox Tester account
3. After login, look for one of these buttons:
   - 📄 **"Premium"** or **"Subscribe"** button on home screen
   - ⭐ **"Upgrade to Premium"** link in menu
   - 🏥 **"Family Plan"** button in settings/subscription screen

4. **Tap the Premium/Subscribe button**

---

## Step 2: Verify Product Information Loads

When you tap the subscription button, you should see:

### ✅ Expected Screen 1: Subscription Details
```
Safeguardian Premium Monthly
Display Price: $4.99/month (or your region's price)

Safeguardian Premium Yearly  
Display Price: $39.99/year (or your region's price)

[Subscribe Button]
```

### ⚠️ If You See Error Instead
If error message appears:
```
"The product was not found in App Store Connect Sandbox"
```

**This means**:
- Product IDs not created in App Store Connect, OR
- Products not in "Ready to Submit" status, OR
- Paid Apps Agreement not activated for this account

**Action**: See **Troubleshooting** section below.

---

## Step 3: Initiate Test Purchase

1. **Tap "[Subscribe]"** button for monthly plan
2. **Wait** 1-2 seconds for StoreKit payment sheet to appear

### ✅ Expected Screen 2: Apple Payment Sheet
You should see the official **Apple StoreKit payment interface** with:
- Product name: "Safeguardian Premium Monthly"
- Price in your region's currency
- "Subscribe" and "Cancel" buttons
- Small text: "Your subscription will automatically renew"

### ⚠️ If Sheet Doesn't Appear
This indicates StoreKit plugin communication issue:
- Check internet connection (must be active)
- Ensure device has valid cellular/WiFi
- Close and reopen app
- Reinstall app if issue persists

---

## Step 4: Complete Test Purchase

1. **Review the price and terms** on the payment sheet
2. **Tap "Subscribe"** button
3. The payment sheet should close within 1-2 seconds
4. You may see **Face ID / password prompt** - use device's security method

### ✅ Expected Result After Purchase
- App shows: **"Purchase successful"** notification
- Premium features **become available** in app
- User status changes to "Premium Member"
- Subscription appears in **Settings → App Store → Manage Subscriptions**

### ⚠️ Error During Purchase
If error dialog appears, see **Troubleshooting** section.

---

## Step 5: Verify Subscription Restoration

This tests that purchases sync correctly:

1. **Sign out** of app (Account menu → Sign Out)
2. **Sign back in** with same Sandbox Tester account
3. **Tap "Restore Purchases"** button (if shown) or go to subscription screen
4. Wait 2-3 seconds

### ✅ Expected Result
- App recognizes the previous purchase
- Premium status **automatically restored**
- No re-purchase required
- Subscription remains active

---

## Step 6: Test Yearly Plan (Optional but Recommended)

Repeat Steps 3-4 with the **Yearly subscription** if you have time:

1. Go back to subscription screen
2. **Tap [Subscribe]** on "Safeguardian Premium Yearly"
3. Verify yearly price displays (should be ~$39.99)
4. Complete purchase
5. Verify premium status activates

---

## Console Logging for Developers (If Needed)

If you encounter issues and want to see diagnostic logs:

### In Xcode (If Attached to Device):
1. **Product → Scheme → Edit Scheme**
2. **Run → Arguments** → Add: `-com.apple.CoreData.SQLDebug 1`
3. Or enable **Console Output** in Xcode
4. Look for lines starting with: `[StoreKit2Plugin]`

Example good log:
```
[StoreKit2Plugin] Fetching products: ["com.buseakdeniz.safeguardian.sub_family_monthly_v2"]
[StoreKit2Plugin] Successfully fetched 1 products: ["com.buseakdeniz.safeguardian.sub_family_monthly_v2"]
[StoreKit2Bridge] Initiating purchase for: Safeguardian Premium Monthly
[StoreKit2Bridge] Purchase successful for product: com.buseakdeniz.safeguardian.sub_family_monthly_v2
```

Example error log:
```
[StoreKit2Plugin] Fetching products: ["com.buseakdeniz.safeguardian.sub_family_monthly_v2"]
[StoreKit2Bridge] Found 0 product(s)
[StoreKit2Bridge] ERROR: Product not found in App Store Connect
```

---

## Troubleshooting During Testing

### Issue 1: "Product not found in App Store Connect Sandbox"

**Causes**:
- [ ] Product ID doesn't exist in App Store Connect
- [ ] Product not in "Ready to Submit" status
- [ ] Sandbox hasn't synced yet (wait 15+ minutes after creating products)
- [ ] Wrong subscription group or payment method missing

**How to Fix**:
1. Have developers verify in App Store Connect:
   - Go to **App → Subscriptions**
   - Check product exists with correct ID
   - Confirm status is **"Ready to Submit"** (not "Rejected" or "Waiting")
2. Have Account Holder verify:
   - **Users and Access → Agreements, Tax, and Banking**
   - Confirm **Paid Apps Agreement** is active (green checkmark)
3. Clear app cache:
   ```
   Settings → General → iPhone Storage → [Safeguardian] → Delete App
   Reinstall from App Store
   ```
4. Wait 30 minutes and retry

---

### Issue 2: "StoreKit transaction could not be verified"

**Causes**:
- [ ] Incorrect account (using personal Apple ID instead of Sandbox Tester)
- [ ] Transaction wasn't properly signed by Apple servers
- [ ] Network connectivity issue during purchase

**How to Fix**:
1. **Confirm you're using Sandbox Tester account** (not your real Apple ID):
   - Settings → [Your Name] → Account
   - Should show: "Sandbox [username]" (not your regular name)
2. Delete app and reinstall
3. Ensure strong WiFi/cellular connection
4. Try purchase again

---

### Issue 3: Subscription Button Missing or Disabled

**Causes**:
- [ ] App not fully loaded yet (wait 3-5 seconds after login)
- [ ] You're in a demo/limited feature mode
- [ ] App encountered an initialization error

**How to Fix**:
1. Wait 5+ seconds after login
2. Check **Settings → App Store → Manage Subscriptions**
   - If premium already purchased, button may not show
3. If button still missing:
   - Restart app (swipe close and relaunch)
   - Reinstall app if issue persists

---

### Issue 4: Payment Sheet Won't Open

**Causes**:
- [ ] StoreKit not responding (network/server issue)
- [ ] Product ID mismatch
- [ ] iOS version < 15.0

**How to Fix**:
1. Verify iPad is running **iPadOS 15.0 or later**:
   - Settings → General → About → OS Version
2. Ensure strong internet connection
3. Restart iPad
4. Reinstall app
5. Retry purchase

---

### Issue 5: "Pending" Status After Purchase

**This is normal in some cases**:
- Account under age of digital responsibility (parental consent pending)
- Unusual account activity requires verification
- Multiple rapid purchases in short time

**Action**:
- Wait 24-48 hours for Apple to process
- User can check status in Settings → App Store → Manage Subscriptions
- App will show "pending" until verified

---

## What App Does Right (Confirmations)

✅ **Error Handling**: Clear messages when products unavailable
✅ **Entitlements**: In-App Purchase capability is properly signed  
✅ **StoreKit 2**: Uses modern iOS 15+ Apple framework  
✅ **Logging**: Console logs help diagnose issues  
✅ **Fallback**: If StoreKit unavailable, app shows helpful message  
✅ **Restoration**: Purchases persist across app reinstalls  

---

## Product IDs Being Used

The app uses these **exact** product IDs (must exist in App Store Connect):

1. **Monthly Subscription**:
   - ID: `com.buseakdeniz.safeguardian.sub_family_monthly_v2`
   - Billing: Monthly (auto-renewing)
   - Localization: English + Turkish

2. **Yearly Subscription**:
   - ID: `com.buseakdeniz.safeguardian.sub_family_yearly_v2`
   - Billing: Annual (auto-renewing)
   - Localization: English + Turkish

Both must be in **"Ready to Submit"** status to appear in sandbox.

---

## What NOT to Test (Out of Scope)

❌ Attempting to use real payment method (sandbox is for testing only)
❌ Testing with multiple sandbox accounts simultaneously
❌ Attempting to test on non-iPad devices (reviewers use iPad Air)
❌ Trying purchase with no internet connection

---

## Quick Reference Checklist

- [ ] Using Sandbox Tester account (Settings → Account)
- [ ] Product information loads (prices display)
- [ ] Purchase sheet opens
- [ ] Purchase completes without errors
- [ ] Subscription status shows "Active"
- [ ] Restore Purchases works
- [ ] Features unlock after purchase
- [ ] Subscription appears in Settings → App Store

If all items checked ✅, IAP is working correctly!

---

## If Everything Checks Out

**Your verdict**: ✅ **APPROVE** - IAP feature is fully functional in sandbox

---

## If Issues Persist

Have developer:

1. Check Xcode console for exact error message
2. Verify App Store Connect settings match product IDs
3. Request an [App Review Appointment](https://developer.apple.com/contact/app-review/) to discuss directly
4. Contact Apple Developer Support with the specific error code

---

## Timeline

- **Expected testing time**: 5-10 minutes
- **Each purchase**: 1-2 seconds
- **Sandbox transaction processing**: Instant (no real charge)

---

## Questions?

If you encounter any issues not covered in this guide:

1. Document the exact error message
2. Note the step where it failed
3. Check app console/Xcode logs for technical details
4. Contact App Review team with this information

---

**Last Updated**: May 21, 2026  
**App**: Safeguardian v1.0.0 (12)  
**Framework**: StoreKit 2  
**Min iOS**: 15.0+
