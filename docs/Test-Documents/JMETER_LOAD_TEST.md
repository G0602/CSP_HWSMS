# HSMS JMeter Load Test

This document describes the current JMeter artifacts and how they relate to the live API surface.

## Current Files

- `jmeter/HSMS_API_100_users.jmx`
- `jmeter/hsms-jmeter.properties`

## Generator

The JMeter plan is generated from the current backend controllers and DTOs:

```bash
node scripts/generate-jmeter-test-plan.js
```

## Default Generated Behavior

- 100 concurrent users
- 20 second ramp-up
- 1 loop per user
- 250ms think time
- login sampler enabled
- read-only `GET` samplers enabled
- mutating `POST`, `PUT`, and `DELETE` samplers included but disabled by default

## Typical Headless Run

```bash
jmeter -n \
  -t jmeter/HSMS_API_100_users.jmx \
  -q jmeter/hsms-jmeter.properties \
  -l jmeter/results.jtl \
  -e -o jmeter/report
```

## Useful Overrides

```bash
jmeter -n \
  -t jmeter/HSMS_API_100_users.jmx \
  -q jmeter/hsms-jmeter.properties \
  -Jusers=100 \
  -JrampUp=20 \
  -Jloops=2 \
  -Jhost=localhost \
  -Jport=5162
```

## Important Notes

- the plan expects a valid login before protected requests
- property defaults provide path IDs such as `productId`, `saleId`, `supplierId`, and `userId`
- if you enable write traffic, use dedicated test data and avoid shared production-like environments
- validate stock-sensitive sales flows carefully before enabling sale creation under load

## Related Docs

- [TESTING_OVERVIEW.md](./TESTING_OVERVIEW.md)
- [INTEGRATION_TEST_ISSUES.md](./INTEGRATION_TEST_ISSUES.md)
