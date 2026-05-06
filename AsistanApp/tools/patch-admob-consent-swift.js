const fs = require('fs');
const path = require('path');

const filePath = path.join(
  __dirname,
  '..',
  'node_modules',
  '@capacitor-community',
  'admob',
  'ios',
  'Plugin',
  'Consent',
  'ConsentExecutor.swift'
);

if (!fs.existsSync(filePath)) {
  console.log('[admob-patch] ConsentExecutor.swift bulunamadı, atlandı.');
  process.exit(0);
}

let content = fs.readFileSync(filePath, 'utf8');

const replacements = [
  ['UMPRequestParameters()', 'RequestParameters()'],
  ['UMPDebugSettings()', 'DebugSettings()'],
  ['UMPDebugGeography(rawValue: debugGeography) ?? UMPDebugGeography.disabled', 'DebugGeography(rawValue: debugGeography) ?? DebugGeography.disabled'],
  ['parameters.tagForUnderAgeOfConsent = tagForUnderAgeOfConsent', 'parameters.isTaggedForUnderAgeOfConsent = tagForUnderAgeOfConsent'],
  ['UMPConsentInformation.sharedInstance.requestConsentInfoUpdate(', 'ConsentInformation.shared.requestConsentInfoUpdate('],
  ['self.getConsentStatusString(UMPConsentInformation.sharedInstance.consentStatus)', 'self.getConsentStatusString(ConsentInformation.shared.consentStatus)'],
  ['UMPConsentInformation.sharedInstance.formStatus == UMPFormStatus.available', 'ConsentInformation.shared.formStatus == FormStatus.available'],
  ['let formStatus = UMPConsentInformation.sharedInstance.formStatus', 'let formStatus = ConsentInformation.shared.formStatus'],
  ['if formStatus == UMPFormStatus.available {', 'if formStatus == FormStatus.available {'],
  ['UMPConsentForm.load(completionHandler: {form, loadError in', 'ConsentForm.load(with: {form, loadError in'],
  ['if UMPConsentInformation.sharedInstance.consentStatus == UMPConsentStatus.required {', 'if ConsentInformation.shared.consentStatus == ConsentStatus.required {'],
  ['UMPConsentInformation.sharedInstance.reset()', 'ConsentInformation.shared.reset()'],
  ['func getConsentStatusString(_ consentStatus: UMPConsentStatus) -> String {', 'func getConsentStatusString(_ consentStatus: ConsentStatus) -> String {'],
  ['case UMPConsentStatus.required:', 'case ConsentStatus.required:'],
  ['case UMPConsentStatus.notRequired:', 'case ConsentStatus.notRequired:'],
  ['case UMPConsentStatus.obtained:', 'case ConsentStatus.obtained:']
];

let changed = false;
for (const [from, to] of replacements) {
  if (content.includes(from)) {
    content = content.split(from).join(to);
    changed = true;
  }
}

if (changed) {
  fs.writeFileSync(filePath, content, 'utf8');
  console.log('[admob-patch] ConsentExecutor.swift güncellendi.');
} else {
  console.log('[admob-patch] Yama gerekmiyor, dosya güncel.');
}
