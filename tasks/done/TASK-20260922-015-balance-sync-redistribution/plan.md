## Technical Plan
1. **Local Balance Assets**: Create SOs for all 9 balance tables.
2. **Editor Sync Tool**: HTTP download from Google Sheets using export CSV format. Parse with resilient CSV parser.
3. **Validation & Atomic Sync**: Validate schema and constraints. Only save to SOs if all sheets are valid.
4. **Consumer Migration**: Update existing logic to read from these SOs instead of hardcoded C# tables.
5. **Exact-One Base Content**: Starter rooms use `초기 구성`; Room 3+ selects exactly one Product from `제품 등장 풀`. Every room also requires exactly one Wall Outlet.
6. **Transactional Promotion**: Roll back generated equipment and restore the locked hint when required base content cannot be finalized.
7. **Product Redistribution**: After base content succeeds, distribute `n` additions to `n` distinct rooms using Product count, area, weighted pool, placement, reachability, and power rules.
8. **Verification**: Run focused EditMode suites and retain `HUMAN_VERIFY_REQUIRED` until Rooms 1-5 pass Play Mode checks.
