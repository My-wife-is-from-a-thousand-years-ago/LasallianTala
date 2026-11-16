# LassalianLedger
## Temporary accounts for demo development:
Email, Password
- admin@demo.test, Demo@12345
- prof@demo.test, Demo@12345
- ana@demo.test, Demo@12345
- ben@demo.test, Demo@12345

## Updating what Tala knows (the handbook)
Tala answers rule questions only from `BenildeTrack/Data/handbook.json`. Edit that file and restart the app; the database syncs to it on startup.
Each section has `number`, `title`, `keywords` (words students might type) and `text` (paste the real handbook wording).
It currently holds the discipline parts of the Benilde Undergraduate Student Handbook 2025-2030 (offenses 7.8, sanctions 7.9, ID, dress code, attendance). If a rule is not in this file, Tala cannot know it.
