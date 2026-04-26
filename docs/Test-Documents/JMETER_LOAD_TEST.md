# HSMS JMeter Load Test

## Generated Files

- `jmeter/HSMS_API_100_users.jmx`
- `jmeter/hsms-jmeter.properties`

## Generator

Run:

```bash
node scripts/generate-jmeter-test-plan.js
```

The generator scans the backend controllers and creates a JMeter plan for the current API surface.

## Default Behavior

- 100 concurrent users
- 20 second ramp-up
- 1 loop per user
- 250ms think time
- Login request enabled
- Read-only `GET` requests enabled
- Mutating endpoints are included but disabled by default for safer load testing

## Typical Run

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
  -Jport=5162 \
  -Jusername=admin_user \
  -Jpassword=<seeded-admin-password>
```

## Notes

- The plan expects a valid login before protected requests.
- Path variables use the defaults in the properties file: `productId`, `saleId`, `supplierId`, and `userId`.
- If you want write-load testing, open the `.jmx` in JMeter GUI and enable the disabled POST, PUT, and DELETE samplers intentionally.
