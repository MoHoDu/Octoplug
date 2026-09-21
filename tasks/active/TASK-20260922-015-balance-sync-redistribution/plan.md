## Technical Plan
1. **Local Balance Assets**: Create SOs for all 9 balance tables.
2. **Editor Sync Tool**: HTTP download from Google Sheets using export CSV format. Parse with resilient CSV parser.
3. **Validation & Atomic Sync**: Validate schema and constraints. Only save to SOs if all sheets are valid.
4. **Consumer Migration**: Update existing logic to read from these SOs instead of hardcoded C# tables.
5. **Product Redistribution**: Implement logic to distribute additional products to existing rooms based on the new '제품 추가 규칙' and '제품 등장 풀' balances.
