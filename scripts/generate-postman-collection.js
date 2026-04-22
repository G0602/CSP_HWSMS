#!/usr/bin/env node

const fs = require("fs");
const path = require("path");

const rootDir = path.resolve(__dirname, "..");
const controllersDir = path.join(rootDir, "backend", "HSMS.API", "Controllers");
const dtoDir = path.join(rootDir, "backend", "HSMS.Application", "DTOs");
const servicesDir = path.join(rootDir, "frontend", "HWSMS_UI", "src", "services");
const programFile = path.join(rootDir, "backend", "HSMS.API", "Program.cs");
const outputDir = path.join(rootDir, "postman");
const collectionFile = path.join(outputDir, "HSMS_API.postman_collection.json");
const environmentFile = path.join(outputDir, "HSMS_Local.postman_environment.json");

const primitiveTypes = new Set([
  "string",
  "int",
  "int?",
  "decimal",
  "decimal?",
  "double",
  "double?",
  "float",
  "float?",
  "bool",
  "bool?",
  "datetime",
  "datetime?",
  "guid",
  "guid?"
]);

const roleMatrix = {
  InventoryRead: ["Admin", "Manager", "Cashier"],
  InventoryManagerRead: ["Admin", "Manager"],
  InventoryWrite: ["Admin", "Manager"],
  InventoryDelete: ["Admin"],
  SalesCreate: ["Admin", "Manager", "Cashier"],
  SalesRead: ["Admin", "Manager"],
  UsersManage: ["Admin"]
};

function read(filePath) {
  return fs.readFileSync(filePath, "utf8");
}

function ensureDir(dirPath) {
  fs.mkdirSync(dirPath, { recursive: true });
}

function parseAttributes(block) {
  const attrs = [];
  const regex = /\[(.*?)\]/gs;
  for (const match of block.matchAll(regex)) {
    attrs.push(match[1].trim());
  }
  return attrs;
}

function normalizeObjectKeys(value) {
  if (Array.isArray(value)) {
    return value.map((item) => normalizeObjectKeys(item));
  }

  if (!value || typeof value !== "object") {
    return value;
  }

  const normalized = {};
  for (const [key, nestedValue] of Object.entries(value)) {
    const nextKey = /^[a-z][A-Z]{2,}$/.test(key) ? key.toLowerCase() : key;
    normalized[nextKey] = normalizeObjectKeys(nestedValue);
  }

  return normalized;
}

function parseDtos() {
  const dtoMap = {};
  for (const file of fs.readdirSync(dtoDir).filter((name) => name.endsWith(".cs"))) {
    const content = read(path.join(dtoDir, file));
    const classMatch = content.match(/public\s+class\s+(\w+)/);
    if (!classMatch) {
      continue;
    }

    const dtoName = classMatch[1];
    const properties = [];
    const propertyRegex = /public\s+([\w<>?.]+)\s+(\w+)\s*\{\s*get;\s*set;\s*\}/g;
    for (const match of content.matchAll(propertyRegex)) {
      properties.push({
        type: match[1],
        name: match[2]
      });
    }

    dtoMap[dtoName] = properties;
  }

  return dtoMap;
}

function splitParameters(parameterBlock) {
  const parts = [];
  let current = "";
  let depth = 0;

  for (const char of parameterBlock) {
    if (char === "<") depth += 1;
    if (char === ">") depth = Math.max(0, depth - 1);

    if (char === "," && depth === 0) {
      if (current.trim()) {
        parts.push(current.trim());
      }
      current = "";
      continue;
    }

    current += char;
  }

  if (current.trim()) {
    parts.push(current.trim());
  }

  return parts;
}

function parseParameters(parameterBlock) {
  if (!parameterBlock.trim()) {
    return [];
  }

  return splitParameters(parameterBlock).map((part) => {
    const cleaned = part.replace(/\s+/g, " ").trim();
    const fromQuery = cleaned.includes("[FromQuery]");
    const withoutAttributes = cleaned.replace(/\[[^\]]+\]\s*/g, "").trim();
    const pieces = withoutAttributes.split(" ");
    const name = pieces[pieces.length - 1];
    const type = pieces[pieces.length - 2] || "string";

    return { raw: cleaned, fromQuery, type, name };
  });
}

function normalizeControllerSegment(controllerName) {
  return controllerName.replace(/Controller$/, "");
}

function toJsonName(name) {
  if (/^[A-Z0-9]+$/.test(name)) {
    return name.toLowerCase();
  }

  return name.charAt(0).toLowerCase() + name.slice(1);
}

function resolveClassRoute(template, controllerName) {
  return template.replace("[controller]", normalizeControllerSegment(controllerName));
}

function combineRoute(baseRoute, methodRoute) {
  const normalizedBase = baseRoute.replace(/\/+$/, "");
  const normalizedMethod = (methodRoute || "").replace(/^\/+/, "");
  return normalizedMethod ? `${normalizedBase}/${normalizedMethod}` : normalizedBase;
}

function postmanPathSegments(route) {
  return route.split("/").filter(Boolean).map((segment) => {
    const match = segment.match(/^\{(.+?)\}$/);
    return match ? `:${match[1].split(":")[0]}` : segment;
  });
}

function guessSampleValue(type, name, dtoMap) {
  const lowerName = name.toLowerCase();
  const normalizedType = type.toLowerCase();

  if (normalizedType.startsWith("list<")) {
    const innerType = type.slice(type.indexOf("<") + 1, type.lastIndexOf(">"));
    return [buildDtoExample(innerType, dtoMap)];
  }

  if (normalizedType === "string" || normalizedType === "string?") {
    if (lowerName.includes("username")) return "{{username}}";
    if (lowerName.includes("password")) return "{{password}}";
    if (lowerName === "role") return "Admin";
    if (lowerName.includes("sku")) return "SKU-1001";
    if (lowerName.includes("category")) return "Tools";
    if (lowerName.includes("contact")) return "0771234567 / supplier@example.com";
    if (lowerName.includes("reason")) return "Manual stock update";
    if (lowerName.includes("name")) return "Sample Name";
    return `sample_${name}`;
  }

  if (normalizedType === "int" || normalizedType === "int?") {
    if (lowerName.includes("quantity")) return 10;
    if (lowerName.includes("userid")) return 1;
    if (lowerName.includes("productid")) return 1;
    if (lowerName.includes("supplierid")) return 1;
    return 1;
  }

  if (normalizedType === "decimal" || normalizedType === "decimal?" || normalizedType === "double" || normalizedType === "float") {
    return 1000.0;
  }

  if (normalizedType === "bool" || normalizedType === "bool?") {
    return true;
  }

  if (normalizedType === "datetime" || normalizedType === "datetime?") {
    return "2026-04-07T10:00:00Z";
  }

  return `{{${name}}}`;
}

function buildDtoExample(dtoName, dtoMap, seen = new Set()) {
  if (seen.has(dtoName)) {
    return {};
  }

  const properties = dtoMap[dtoName];
  if (!properties) {
    return {};
  }

  seen.add(dtoName);
  const result = {};
  for (const property of properties) {
    const propertyType = property.type.replace(/\?$/, "");
    if (!primitiveTypes.has(property.type.toLowerCase()) && dtoMap[propertyType]) {
      result[toJsonName(property.name)] = buildDtoExample(propertyType, dtoMap, new Set(seen));
      continue;
    }

    result[toJsonName(property.name)] = guessSampleValue(property.type, property.name, dtoMap);
  }

  return result;
}

function isAnonymous(attributes) {
  return attributes.some((attr) => attr.startsWith("AllowAnonymous"));
}

function getHttpMeta(attributes) {
  for (const attr of attributes) {
    const match = attr.match(/^(Http(Get|Post|Put|Delete|Patch))(?:\("([^"]*)"\))?/);
    if (match) {
      return {
        method: match[2].toUpperCase(),
        route: match[3] || ""
      };
    }
  }

  return null;
}

function getPolicy(attributes) {
  for (const attr of attributes) {
    const match = attr.match(/Authorize\(Policy\s*=\s*AuthPolicies\.(\w+)\)/);
    if (match) {
      return match[1];
    }
  }

  return null;
}

function collectClassRoutes(content, controllerName, classIndex) {
  const routeWindow = content.slice(Math.max(0, classIndex - 500), classIndex);
  return [...routeWindow.matchAll(/\[Route\("([^"]+)"\)\]/g)]
    .map((match) => resolveClassRoute(match[1], controllerName));
}

function extractControllerData(dtoMap) {
  const controllers = [];
  const files = fs.readdirSync(controllersDir).filter((name) => name.endsWith(".cs")).sort();

  for (const file of files) {
    const content = read(path.join(controllersDir, file));
    const classMatch = content.match(/((?:\s*\[[^\]]+\]\s*)*)public\s+class\s+(\w+)/s);
    if (!classMatch) {
      continue;
    }

    const classAttributes = parseAttributes(classMatch[1]);
    const controllerName = classMatch[2];
    const classDeclarationIndex = classMatch.index + classMatch[0].indexOf("public class");
    const classRoutes = collectClassRoutes(content, controllerName, classDeclarationIndex);

    const preferredRoute = `api/${normalizeControllerSegment(controllerName)}`;
    const canonicalRoute = classRoutes.find((route) => route === preferredRoute) || classRoutes[0];
    const methodRegex = /((?:\s*\[[^\]]+\]\s*)*)public\s+async\s+Task<IActionResult>\s+(\w+)\(([\s\S]*?)\)\s*\{/g;
    const endpoints = [];

    for (const match of content.matchAll(methodRegex)) {
      const attributes = parseAttributes(match[1]);
      const methodName = match[2];
      const parameters = parseParameters(match[3]);
      const httpMeta = getHttpMeta(attributes);

      if (!httpMeta || !canonicalRoute) {
        continue;
      }

      const route = combineRoute(canonicalRoute, httpMeta.route);
      const pathSegments = postmanPathSegments(route);
      const routeParamNames = [...route.matchAll(/\{(.+?)\}/g)].map((entry) => entry[1].split(":")[0]);
      const queryParams = parameters.filter((param) => param.fromQuery);
      const bodyParam = parameters.find((param) => !param.fromQuery && !routeParamNames.includes(param.name) && !primitiveTypes.has(param.type.toLowerCase()));
      const body = bodyParam ? buildDtoExample(bodyParam.type.replace(/\?$/, ""), dtoMap) : null;
      const policy = getPolicy(attributes);
      const anonymous = isAnonymous(attributes);

      endpoints.push({
        controllerName,
        file,
        methodName,
        method: httpMeta.method,
        route,
        pathSegments,
        routeParamNames,
        queryParams,
        body,
        policy,
        anonymous,
        aliases: classRoutes
          .filter((alias) => alias !== canonicalRoute)
          .map((alias) => combineRoute(alias, httpMeta.route))
      });
    }

    controllers.push({
      controllerName,
      folderName: normalizeControllerSegment(controllerName),
      endpoints
    });
  }

  return controllers;
}

function detectFrontendUsage() {
  const usage = {};
  if (!fs.existsSync(servicesDir)) {
    return usage;
  }

  for (const file of fs.readdirSync(servicesDir).filter((name) => name.endsWith(".ts"))) {
    const content = read(path.join(servicesDir, file));
    for (const match of content.matchAll(/`?\$\{API_BASE_URL\}(\/api\/[A-Za-z0-9/_-]+)`?/g)) {
      usage[match[1]] = usage[match[1]] || [];
      usage[match[1]].push(file);
    }
  }

  return usage;
}

function makeDescription(endpoint, frontendUsage) {
  const lines = [];
  lines.push(`Source: ${endpoint.file} -> ${endpoint.methodName}`);
  lines.push(`Route: /${endpoint.route}`);

  if (endpoint.aliases.length > 0) {
    lines.push(`Aliases: ${endpoint.aliases.map((alias) => `/${alias}`).join(", ")}`);
  }

  if (endpoint.anonymous) {
    lines.push("Auth: Anonymous access allowed");
  } else if (endpoint.policy) {
    const roles = roleMatrix[endpoint.policy] || [];
    lines.push(`Auth Policy: ${endpoint.policy}`);
    lines.push(`Allowed Roles: ${roles.join(", ")}`);
  }

  const frontendHits = Object.entries(frontendUsage)
    .filter(([route]) => endpoint.route.startsWith(route.replace(/^\/+/, "")))
    .flatMap(([, files]) => files);

  if (frontendHits.length > 0) {
    lines.push(`Frontend Usage: ${[...new Set(frontendHits)].join(", ")}`);
  }

  return lines.join("\n");
}

function buildUrl(endpoint) {
  const query = endpoint.queryParams.map((param) => ({
    key: param.name,
    value: queryDefaultValue(param.name),
    description: `Query parameter: ${param.name}`
  }));
  const normalizedPath = endpoint.pathSegments.join("/");

  return {
    raw: `{{baseUrl}}/${normalizedPath}`,
    host: ["{{baseUrl}}"],
    path: endpoint.pathSegments,
    query
  };
}

function queryDefaultValue(name) {
  const lower = name.toLowerCase();
  if (lower === "query") return "hammer";
  if (lower === "limit") return "20";
  if (lower === "type") return "daily";
  if (lower === "transactionid") return "{{saleId}}";
  if (lower === "fromdate") return "2026-04-01";
  if (lower === "todate") return "2026-04-30";
  return `{{${name}}}`;
}

function buildPrerequest(endpoint) {
  if (endpoint.anonymous) {
    return "";
  }

  return [
    "const token = pm.environment.get('accessToken');",
    "if (token) {",
    "  pm.request.headers.upsert({ key: 'Authorization', value: `Bearer ${token}` });",
    "}"
  ].join("\n");
}

function buildTests(endpoint) {
  const route = endpoint.route.toLowerCase();
  const lines = [
    "pm.test('Response status is expected', function () {",
    "  pm.expect(pm.response.code).to.be.oneOf([200, 201]);",
    "});"
  ];

  if (endpoint.method === "DELETE") {
    lines.push(
      "pm.test('Delete call returns a confirmation message', function () {",
      "  pm.expect(pm.response.text()).to.not.equal('');",
      "});"
    );
  } else {
    lines.push(
      "pm.test('Response is JSON when applicable', function () {",
      "  const contentType = pm.response.headers.get('Content-Type') || '';",
      "  pm.expect(contentType.length > 0).to.equal(true);",
      "});"
    );
  }

  if (route === "api/auth/login" || route === "api/auth/register") {
    lines.push(
      "const json = pm.response.json();",
      "pm.test('Auth response includes user and token data', function () {",
      "  pm.expect(json).to.have.property('accessToken');",
      "  pm.expect(json).to.have.property('username');",
      "  pm.expect(json).to.have.property('role');",
      "});",
      "if (json && json.accessToken) {",
      "  pm.environment.set('accessToken', json.accessToken);",
      "  pm.environment.set('currentUserId', String(json.userId || ''));",
      "  pm.environment.set('currentUsername', json.username || '');",
      "  pm.environment.set('currentUserRole', json.role || '');",
      "}"
    );
  }

  if (route === "api/suppliers" && endpoint.method === "POST") {
    lines.push(
      "const json = pm.response.json();",
      "if (json && json.id) { pm.environment.set('supplierId', String(json.id)); }"
    );
  }

  if (route === "api/users" && endpoint.method === "POST") {
    lines.push(
      "const json = pm.response.json();",
      "if (json && json.id) { pm.environment.set('userId', String(json.id)); }"
    );
  }

  if (route === "api/sales" && endpoint.method === "POST") {
    lines.push(
      "const json = pm.response.json();",
      "pm.test('Sale response includes transaction totals', function () {",
      "  pm.expect(json).to.have.property('saleId');",
      "  pm.expect(json).to.have.property('totalAmount');",
      "  pm.expect(json.totalAmount).to.be.a('number');",
      "});",
      "if (json && json.saleId) { pm.environment.set('saleId', String(json.saleId)); }"
    );
  }

  if (route === "api/reports/summary") {
    lines.push(
      "const json = pm.response.json();",
      "pm.test('Reports summary includes all sections', function () {",
      "  pm.expect(json).to.have.property('daily');",
      "  pm.expect(json).to.have.property('monthly');",
      "  pm.expect(json).to.have.property('lowStock');",
      "});"
    );
  }

  if (route === "api/reports/analytics") {
    lines.push(
      "const json = pm.response.json();",
      "pm.test('Analytics response includes sales and profit metrics', function () {",
      "  pm.expect(json).to.have.property('totalSales');",
      "  pm.expect(json).to.have.property('totalProfit');",
      "  pm.expect(json).to.have.property('dailyTrends');",
      "  pm.expect(json).to.have.property('monthlyTrends');",
      "});"
    );
  }

  if (route === "api/reports/export") {
    lines.push(
      "pm.test('CSV export returns downloadable content', function () {",
      "  pm.expect(pm.response.headers.get('Content-Type') || '').to.include('text/csv');",
      "  pm.expect(pm.response.text()).to.include(',');",
      "});"
    );
  }

  if (route === "api/health") {
    return [
      "pm.test('Health endpoint returns 200', function () {",
      "  pm.response.to.have.status(200);",
      "});",
      "pm.test('Health payload contains status', function () {",
      "  const json = pm.response.json();",
      "  pm.expect(json).to.have.property('status');",
      "});"
    ].join("\n");
  }

  return lines.join("\n");
}

function buildRequestBody(endpoint) {
  if (!endpoint.body) {
    return undefined;
  }

  return {
    mode: "raw",
    raw: JSON.stringify(normalizeObjectKeys(endpoint.body), null, 2),
    options: {
      raw: {
        language: "json"
      }
    }
  };
}

function buildItems(controllers, frontendUsage) {
  return controllers
    .filter((controller) => controller.endpoints.length > 0)
    .map((controller) => ({
      name: controller.folderName,
      item: controller.endpoints.map((endpoint) => ({
        name: `${endpoint.method} /${endpoint.route}`,
        request: {
          method: endpoint.method,
          header: endpoint.body ? [{ key: "Content-Type", value: "application/json" }] : [],
          description: makeDescription(endpoint, frontendUsage),
          url: buildUrl(endpoint),
          body: buildRequestBody(endpoint)
        },
        event: [
          {
            listen: "prerequest",
            script: {
              type: "text/javascript",
              exec: buildPrerequest(endpoint).split("\n").filter(Boolean)
            }
          },
          {
            listen: "test",
            script: {
              type: "text/javascript",
              exec: buildTests(endpoint).split("\n").filter(Boolean)
            }
          }
        ]
      }))
    }));
}

function buildHealthEndpoint() {
  if (!fs.existsSync(programFile)) {
    return null;
  }

  const content = read(programFile);
  if (!content.includes('app.MapHealthChecks("/api/health"')) {
    return null;
  }

  return {
    controllerName: "Health",
    folderName: "Health",
    endpoints: [
      {
        controllerName: "Health",
        file: "Program.cs",
        methodName: "MapHealthChecks",
        method: "GET",
        route: "api/health",
        pathSegments: ["api", "health"],
        routeParamNames: [],
        queryParams: [],
        body: null,
        policy: null,
        anonymous: true,
        aliases: []
      }
    ]
  };
}

function buildCollection(controllers, frontendUsage) {
  return {
    info: {
      name: "HSMS API",
      description: "Generated by scripts/generate-postman-collection.js by scanning controllers, DTOs, Program.cs, and frontend service usage.",
      schema: "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
    },
    auth: {
      type: "bearer",
      bearer: [
        {
          key: "token",
          value: "{{accessToken}}",
          type: "string"
        }
      ]
    },
    event: [
      {
        listen: "prerequest",
        script: {
          type: "text/javascript",
          exec: [
            "pm.variables.set('timestamp', String(Date.now()));"
          ]
        }
      }
    ],
    variable: [
      { key: "baseUrl", value: "http://localhost:5162" },
      { key: "accessToken", value: "" },
      { key: "username", value: "admin_user" },
      { key: "password", value: "Admin@123" },
      { key: "productId", value: "1" },
      { key: "supplierId", value: "1" },
      { key: "saleId", value: "1" },
      { key: "userId", value: "1" }
    ],
    item: buildItems(controllers, frontendUsage)
  };
}

function buildEnvironment() {
  return {
    name: "HSMS Local",
    values: [
      { key: "baseUrl", value: "http://localhost:5162", type: "default", enabled: true },
      { key: "accessToken", value: "", type: "secret", enabled: true },
      { key: "username", value: "admin_user", type: "default", enabled: true },
      { key: "password", value: "Admin@123", type: "secret", enabled: true },
      { key: "productId", value: "1", type: "default", enabled: true },
      { key: "supplierId", value: "1", type: "default", enabled: true },
      { key: "saleId", value: "1", type: "default", enabled: true },
      { key: "userId", value: "1", type: "default", enabled: true },
      { key: "currentUserId", value: "", type: "default", enabled: true },
      { key: "currentUsername", value: "", type: "default", enabled: true },
      { key: "currentUserRole", value: "", type: "default", enabled: true }
    ],
    _postman_variable_scope: "environment",
    _postman_exported_at: new Date().toISOString(),
    _postman_exported_using: "Codex project scanner"
  };
}

function main() {
  ensureDir(outputDir);

  const dtoMap = parseDtos();
  const controllers = extractControllerData(dtoMap);
  const healthController = buildHealthEndpoint();
  if (healthController) {
    controllers.unshift(healthController);
  }
  const frontendUsage = detectFrontendUsage();

  const collection = buildCollection(controllers, frontendUsage);
  const environment = buildEnvironment();

  fs.writeFileSync(collectionFile, JSON.stringify(collection, null, 2) + "\n");
  fs.writeFileSync(environmentFile, JSON.stringify(environment, null, 2) + "\n");

  const endpointCount = controllers.reduce((sum, controller) => sum + controller.endpoints.length, 0);
  console.log(`Generated ${collectionFile}`);
  console.log(`Generated ${environmentFile}`);
  console.log(`Scanned ${controllers.length} API groups and ${endpointCount} endpoints.`);
}

main();
