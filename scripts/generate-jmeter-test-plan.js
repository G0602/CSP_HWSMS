#!/usr/bin/env node

const fs = require("fs");
const path = require("path");

const rootDir = path.resolve(__dirname, "..");
const controllersDir = path.join(rootDir, "backend", "HSMS.API", "Controllers");
const dtoDir = path.join(rootDir, "backend", "HSMS.Application", "DTOs");
const programFile = path.join(rootDir, "backend", "HSMS.API", "Program.cs");
const devSettingsFile = path.join(rootDir, "backend", "HSMS.API", "appsettings.Development.json");
const outputDir = path.join(rootDir, "jmeter");
const jmxFile = path.join(outputDir, "HSMS_API_100_users.jmx");
const propertiesFile = path.join(outputDir, "hsms-jmeter.properties");

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

function read(filePath) {
  return fs.readFileSync(filePath, "utf8");
}

function ensureDir(dirPath) {
  fs.mkdirSync(dirPath, { recursive: true });
}

function xmlEscape(value) {
  return String(value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&apos;");
}

function parseAttributes(block) {
  const attrs = [];
  const regex = /\[(.*?)\]/gs;
  for (const match of block.matchAll(regex)) {
    attrs.push(match[1].trim());
  }
  return attrs;
}

function parseDtos() {
  const dtoMap = {};
  for (const file of fs.readdirSync(dtoDir).filter((name) => name.endsWith(".cs"))) {
    const content = read(path.join(dtoDir, file));
    const classMatch = content.match(/public\s+class\s+(\w+)/);
    if (!classMatch) {
      continue;
    }

    const properties = [];
    const propertyRegex = /public\s+([\w<>?.]+)\s+(\w+)\s*\{\s*get;\s*set;\s*\}/g;
    for (const match of content.matchAll(propertyRegex)) {
      properties.push({
        type: match[1],
        name: match[2]
      });
    }

    dtoMap[classMatch[1]] = properties;
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
    return {
      raw: cleaned,
      fromQuery,
      type: pieces[pieces.length - 2] || "string",
      name: pieces[pieces.length - 1]
    };
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

function collectClassRoutes(content, controllerName, classIndex) {
  const routeWindow = content.slice(Math.max(0, classIndex - 500), classIndex);
  return [...routeWindow.matchAll(/\[Route\("([^"]+)"\)\]/g)]
    .map((match) => resolveClassRoute(match[1], controllerName));
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

function guessSampleValue(type, name, dtoMap) {
  const lowerName = name.toLowerCase();
  const normalizedType = type.toLowerCase();

  if (normalizedType.startsWith("list<")) {
    const innerType = type.slice(type.indexOf("<") + 1, type.lastIndexOf(">"));
    return [buildDtoExample(innerType, dtoMap)];
  }

  if (normalizedType === "string" || normalizedType === "string?") {
    if (lowerName.includes("username")) return "${username}";
    if (lowerName.includes("password")) return "${password}";
    if (lowerName === "role") return "Admin";
    if (lowerName.includes("sku")) return "SKU-1001";
    if (lowerName.includes("category")) return "Tools";
    if (lowerName.includes("contact")) return "0771234567 / supplier@example.com";
    if (lowerName.includes("reason")) return "Load test stock update";
    if (lowerName.includes("name")) return "Sample Name";
    return `sample_${name}`;
  }

  if (normalizedType === "int" || normalizedType === "int?") {
    if (lowerName.includes("productid")) return "${productId}";
    if (lowerName.includes("supplierid")) return "${supplierId}";
    if (lowerName.includes("quantity")) return 1;
    if (lowerName.includes("userid")) return "${userId}";
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

  return `\${${name}}`;
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

function getDefaultConfig() {
  let protocol = "http";
  let host = "localhost";
  let port = "5162";

  if (fs.existsSync(devSettingsFile)) {
    try {
      const json = JSON.parse(read(devSettingsFile));
      const baseUrl = json.BACKEND_PUBLIC_URL || "http://localhost:5162/";
      const url = new URL(baseUrl);
      protocol = url.protocol.replace(":", "");
      host = url.hostname;
      port = url.port || (protocol === "https" ? "443" : "80");
    } catch {
      // keep defaults
    }
  }

  return { protocol, host, port };
}

function parseControllers(dtoMap) {
  const controllers = [];
  const files = fs.readdirSync(controllersDir).filter((name) => name.endsWith(".cs")).sort();

  for (const file of files) {
    const content = read(path.join(controllersDir, file));
    const classMatch = content.match(/((?:\s*\[[^\]]+\]\s*)*)public\s+class\s+(\w+)/s);
    if (!classMatch) {
      continue;
    }

    const controllerName = classMatch[2];
    const classDeclarationIndex = classMatch.index + classMatch[0].indexOf("public class");
    const classRoutes = collectClassRoutes(content, controllerName, classDeclarationIndex);
    const preferredRoute = `api/${normalizeControllerSegment(controllerName)}`;
    const canonicalRoute = classRoutes.find((route) => route === preferredRoute) || classRoutes[0];
    if (!canonicalRoute) {
      continue;
    }

    const endpoints = [];
    const methodRegex = /((?:\s*\[[^\]]+\]\s*)*)public\s+async\s+Task<IActionResult>\s+(\w+)\(([\s\S]*?)\)\s*\{/g;
    for (const match of content.matchAll(methodRegex)) {
      const attributes = parseAttributes(match[1]);
      const httpMeta = getHttpMeta(attributes);
      if (!httpMeta) {
        continue;
      }

      const route = combineRoute(canonicalRoute, httpMeta.route);
      const parameters = parseParameters(match[3]);
      const routeParamNames = [...route.matchAll(/\{(.+?)\}/g)].map((entry) => entry[1].split(":")[0]);
      const queryParams = parameters.filter((param) => param.fromQuery);
      const bodyParam = parameters.find((param) => !param.fromQuery && !routeParamNames.includes(param.name) && !primitiveTypes.has(param.type.toLowerCase()));

      endpoints.push({
        methodName: match[2],
        controllerName: normalizeControllerSegment(controllerName),
        method: httpMeta.method,
        route,
        queryParams,
        body: bodyParam ? buildDtoExample(bodyParam.type.replace(/\?$/, ""), dtoMap) : null,
        anonymous: isAnonymous(attributes)
      });
    }

    controllers.push(...endpoints);
  }

  if (fs.existsSync(programFile) && read(programFile).includes('app.MapHealthChecks("/api/health"')) {
    controllers.unshift({
      methodName: "HealthCheck",
      controllerName: "Health",
      method: "GET",
      route: "api/health",
      queryParams: [],
      body: null,
      anonymous: true
    });
  }

  return controllers;
}

function routeToPath(route) {
  return "/" + route.split("/").filter(Boolean).map((segment) => {
    const routeParam = segment.match(/^\{(.+?)\}$/);
    if (!routeParam) {
      return segment;
    }

    const name = routeParam[1].split(":")[0];
    return `\${${name}}`;
  }).join("/");
}

function queryValue(name) {
  const lower = name.toLowerCase();
  if (lower === "query") return "hammer";
  if (lower === "limit") return "20";
  if (lower === "type") return "daily";
  if (lower === "transactionid") return "${saleId}";
  if (lower === "fromdate") return "2026-04-01";
  if (lower === "todate") return "2026-04-30";
  return `\${${name}}`;
}

function buildFullPath(endpoint) {
  const basePath = routeToPath(endpoint.route);
  if (endpoint.queryParams.length === 0) {
    return basePath;
  }

  const query = endpoint.queryParams
    .map((param) => `${encodeURIComponent(param.name)}=${queryValue(param.name)}`)
    .join("&");

  return `${basePath}?${query}`;
}

function isEnabledLoadEndpoint(endpoint) {
  const route = endpoint.route.toLowerCase();

  if (route === "api/auth/login") return true;
  if (route === "api/sales" && endpoint.method === "POST") return true;
  if (route === "api/reports/summary") return true;
  if (route === "api/reports/analytics") return true;
  if (route === "api/reports/export") return true;
  if (route === "api/health") return true;

  return false;
}

function buildAssertions(endpoint) {
  const successCodes = endpoint.route.toLowerCase() === "api/reports/export" ? "200" : "200|201";
  const bodyChecks = [];

  if (endpoint.route.toLowerCase() === "api/auth/login") {
    bodyChecks.push("accessToken");
  }

  if (endpoint.route.toLowerCase() === "api/sales" && endpoint.method === "POST") {
    bodyChecks.push("saleId", "totalAmount");
  }

  if (endpoint.route.toLowerCase() === "api/reports/summary") {
    bodyChecks.push("daily", "monthly", "lowStock");
  }

  if (endpoint.route.toLowerCase() === "api/reports/analytics") {
    bodyChecks.push("totalSales", "totalProfit");
  }

  const responseCodeAssertion = `
        <ResponseAssertion guiclass="AssertionGui" testclass="ResponseAssertion" testname="Assert ${xmlEscape(endpoint.methodName)} response code" enabled="true">
          <collectionProp name="Asserion.test_strings">
            <stringProp name="expectedCode">${successCodes}</stringProp>
          </collectionProp>
          <stringProp name="Assertion.custom_message"></stringProp>
          <stringProp name="Assertion.test_field">Assertion.response_code</stringProp>
          <boolProp name="Assertion.assume_success">false</boolProp>
          <intProp name="Assertion.test_type">1</intProp>
        </ResponseAssertion>
        <hashTree/>`;

  const bodyAssertions = bodyChecks.map((check) => `
        <ResponseAssertion guiclass="AssertionGui" testclass="ResponseAssertion" testname="Assert ${xmlEscape(endpoint.methodName)} contains ${xmlEscape(check)}" enabled="true">
          <collectionProp name="Asserion.test_strings">
            <stringProp name="${xmlEscape(check)}">${xmlEscape(check)}</stringProp>
          </collectionProp>
          <stringProp name="Assertion.custom_message"></stringProp>
          <stringProp name="Assertion.test_field">Assertion.response_data</stringProp>
          <boolProp name="Assertion.assume_success">false</boolProp>
          <intProp name="Assertion.test_type">2</intProp>
        </ResponseAssertion>
        <hashTree/>`).join("");

  const durationAssertion = `
        <DurationAssertion guiclass="DurationAssertionGui" testclass="DurationAssertion" testname="Assert ${xmlEscape(endpoint.methodName)} under SLA" enabled="true">
          <stringProp name="DurationAssertion.duration">\${__P(maxResponseMs,2000)}</stringProp>
        </DurationAssertion>
        <hashTree/>`;

  return `${responseCodeAssertion}${bodyAssertions}${durationAssertion}`;
}

function buildHttpSampler(endpoint, index) {
  const enabled = isEnabledLoadEndpoint(endpoint);
  const body = endpoint.body ? JSON.stringify(endpoint.body, null, 2) : "";
  const bodyXml = endpoint.body
    ? `
          <boolProp name="HTTPSampler.postBodyRaw">true</boolProp>
          <elementProp name="HTTPsampler.Arguments" elementType="Arguments">
            <collectionProp name="Arguments.arguments">
              <elementProp name="" elementType="HTTPArgument">
                <boolProp name="HTTPArgument.always_encode">false</boolProp>
                <stringProp name="Argument.value">${xmlEscape(body)}</stringProp>
                <stringProp name="Argument.metadata">=</stringProp>
              </elementProp>
            </collectionProp>
          </elementProp>`
    : `
          <elementProp name="HTTPsampler.Arguments" elementType="Arguments">
            <collectionProp name="Arguments.arguments"/>
          </elementProp>`;

  const authHeader = endpoint.anonymous ? "" : `
        <HeaderManager guiclass="HeaderPanel" testclass="HeaderManager" testname="Headers - ${xmlEscape(endpoint.methodName)}" enabled="true">
          <collectionProp name="HeaderManager.headers">
            <elementProp name="" elementType="Header">
              <stringProp name="Header.name">Authorization</stringProp>
              <stringProp name="Header.value">Bearer \${accessToken}</stringProp>
            </elementProp>
            <elementProp name="" elementType="Header">
              <stringProp name="Header.name">Content-Type</stringProp>
              <stringProp name="Header.value">application/json</stringProp>
            </elementProp>
          </collectionProp>
        </HeaderManager>
        <hashTree/>`;

  const contentTypeHeader = endpoint.anonymous && endpoint.body ? `
        <HeaderManager guiclass="HeaderPanel" testclass="HeaderManager" testname="Headers - ${xmlEscape(endpoint.methodName)}" enabled="true">
          <collectionProp name="HeaderManager.headers">
            <elementProp name="" elementType="Header">
              <stringProp name="Header.name">Content-Type</stringProp>
              <stringProp name="Header.value">application/json</stringProp>
            </elementProp>
          </collectionProp>
        </HeaderManager>
        <hashTree/>` : "";

  const tokenExtractor = endpoint.route === "api/Auth/login" ? `
        <JSONPostProcessor guiclass="JSONPostProcessorGui" testclass="JSONPostProcessor" testname="Extract Access Token" enabled="true">
          <stringProp name="JSONPostProcessor.referenceNames">accessToken</stringProp>
          <stringProp name="JSONPostProcessor.jsonPathExprs">$.accessToken</stringProp>
          <stringProp name="JSONPostProcessor.match_numbers">1</stringProp>
          <stringProp name="JSONPostProcessor.defaultValues"></stringProp>
          <boolProp name="JSONPostProcessor.compute_concat">false</boolProp>
        </JSONPostProcessor>
        <hashTree/>` : "";

  return `
      <HTTPSamplerProxy guiclass="HttpTestSampleGui" testclass="HTTPSamplerProxy" testname="${xmlEscape(`${index}. ${endpoint.method} ${endpoint.route}`)}" enabled="${enabled ? "true" : "false"}">
        ${bodyXml}
        <stringProp name="HTTPSampler.domain">\${host}</stringProp>
        <stringProp name="HTTPSampler.port">\${port}</stringProp>
        <stringProp name="HTTPSampler.protocol">\${protocol}</stringProp>
        <stringProp name="HTTPSampler.path">${xmlEscape(buildFullPath(endpoint))}</stringProp>
        <stringProp name="HTTPSampler.method">${endpoint.method}</stringProp>
        <boolProp name="HTTPSampler.follow_redirects">true</boolProp>
        <boolProp name="HTTPSampler.auto_redirects">false</boolProp>
        <boolProp name="HTTPSampler.use_keepalive">true</boolProp>
        <boolProp name="HTTPSampler.DO_MULTIPART_POST">false</boolProp>
      </HTTPSamplerProxy>
      <hashTree>${authHeader}${contentTypeHeader}${tokenExtractor}${buildAssertions(endpoint)}</hashTree>`;
}

function buildJmx(endpoints, defaults) {
  const samplers = endpoints.map((endpoint, index) => buildHttpSampler(endpoint, index + 1)).join("\n");

  return `<?xml version="1.0" encoding="UTF-8"?>
<jmeterTestPlan version="1.2" properties="5.0" jmeter="5.6.3">
  <hashTree>
    <TestPlan guiclass="TestPlanGui" testclass="TestPlan" testname="HSMS API Load Test - 100 Concurrent Users" enabled="true">
      <stringProp name="TestPlan.comments">Generated from project controllers. Login and read endpoints are enabled by default. Mutating endpoints are generated but disabled for safer load testing.</stringProp>
      <boolProp name="TestPlan.functional_mode">false</boolProp>
      <boolProp name="TestPlan.tearDown_on_shutdown">true</boolProp>
      <boolProp name="TestPlan.serialize_threadgroups">false</boolProp>
      <elementProp name="TestPlan.user_defined_variables" elementType="Arguments" guiclass="ArgumentsPanel" testclass="Arguments" testname="Variables" enabled="true">
        <collectionProp name="Arguments.arguments">
          <elementProp name="protocol" elementType="Argument">
            <stringProp name="Argument.name">protocol</stringProp>
            <stringProp name="Argument.value">\${__P(protocol,${defaults.protocol})}</stringProp>
            <stringProp name="Argument.metadata">=</stringProp>
          </elementProp>
          <elementProp name="host" elementType="Argument">
            <stringProp name="Argument.name">host</stringProp>
            <stringProp name="Argument.value">\${__P(host,${defaults.host})}</stringProp>
            <stringProp name="Argument.metadata">=</stringProp>
          </elementProp>
          <elementProp name="port" elementType="Argument">
            <stringProp name="Argument.name">port</stringProp>
            <stringProp name="Argument.value">\${__P(port,${defaults.port})}</stringProp>
            <stringProp name="Argument.metadata">=</stringProp>
          </elementProp>
          <elementProp name="username" elementType="Argument">
            <stringProp name="Argument.name">username</stringProp>
            <stringProp name="Argument.value">\${__P(username,admin_user)}</stringProp>
            <stringProp name="Argument.metadata">=</stringProp>
          </elementProp>
          <elementProp name="password" elementType="Argument">
            <stringProp name="Argument.name">password</stringProp>
            <stringProp name="Argument.value">\${__P(password,change_admin_password)}</stringProp>
            <stringProp name="Argument.metadata">=</stringProp>
          </elementProp>
          <elementProp name="productId" elementType="Argument">
            <stringProp name="Argument.name">productId</stringProp>
            <stringProp name="Argument.value">\${__P(productId,1)}</stringProp>
            <stringProp name="Argument.metadata">=</stringProp>
          </elementProp>
          <elementProp name="supplierId" elementType="Argument">
            <stringProp name="Argument.name">supplierId</stringProp>
            <stringProp name="Argument.value">\${__P(supplierId,1)}</stringProp>
            <stringProp name="Argument.metadata">=</stringProp>
          </elementProp>
          <elementProp name="saleId" elementType="Argument">
            <stringProp name="Argument.name">saleId</stringProp>
            <stringProp name="Argument.value">\${__P(saleId,1)}</stringProp>
            <stringProp name="Argument.metadata">=</stringProp>
          </elementProp>
          <elementProp name="userId" elementType="Argument">
            <stringProp name="Argument.name">userId</stringProp>
            <stringProp name="Argument.value">\${__P(userId,1)}</stringProp>
            <stringProp name="Argument.metadata">=</stringProp>
          </elementProp>
        </collectionProp>
      </elementProp>
      <stringProp name="TestPlan.user_define_classpath"></stringProp>
    </TestPlan>
    <hashTree>
      <ThreadGroup guiclass="ThreadGroupGui" testclass="ThreadGroup" testname="100 Concurrent Users" enabled="true">
        <stringProp name="ThreadGroup.on_sample_error">continue</stringProp>
        <elementProp name="ThreadGroup.main_controller" elementType="LoopController" guiclass="LoopControlPanel" testclass="LoopController" testname="Loop Controller" enabled="true">
          <boolProp name="LoopController.continue_forever">false</boolProp>
          <stringProp name="LoopController.loops">\${__P(loops,1)}</stringProp>
        </elementProp>
        <stringProp name="ThreadGroup.num_threads">\${__P(users,100)}</stringProp>
        <stringProp name="ThreadGroup.ramp_time">\${__P(rampUp,20)}</stringProp>
        <boolProp name="ThreadGroup.same_user_on_next_iteration">true</boolProp>
        <stringProp name="ThreadGroup.scheduler">false</stringProp>
        <stringProp name="ThreadGroup.duration"></stringProp>
        <stringProp name="ThreadGroup.delay"></stringProp>
      </ThreadGroup>
      <hashTree>
        <CookieManager guiclass="CookiePanel" testclass="CookieManager" testname="Cookie Manager" enabled="true">
          <collectionProp name="CookieManager.cookies"/>
          <boolProp name="CookieManager.clearEachIteration">false</boolProp>
          <boolProp name="CookieManager.controlledByThreadGroup">false</boolProp>
        </CookieManager>
        <hashTree/>
        <CacheManager guiclass="CacheManagerGui" testclass="CacheManager" testname="Cache Manager" enabled="true">
          <boolProp name="clearEachIteration">false</boolProp>
          <boolProp name="useExpires">true</boolProp>
        </CacheManager>
        <hashTree/>
        <ConstantTimer guiclass="ConstantTimerGui" testclass="ConstantTimer" testname="Think Time 250ms" enabled="true">
          <stringProp name="ConstantTimer.delay">\${__P(thinkTimeMs,250)}</stringProp>
        </ConstantTimer>
        <hashTree/>
        ${samplers}
        <ResultCollector guiclass="SummaryReport" testclass="ResultCollector" testname="Summary Report" enabled="true">
          <boolProp name="ResultCollector.error_logging">false</boolProp>
          <objProp>
            <name>saveConfig</name>
            <value class="SampleSaveConfiguration">
              <time>true</time>
              <latency>true</latency>
              <timestamp>true</timestamp>
              <success>true</success>
              <label>true</label>
              <code>true</code>
              <message>true</message>
              <threadName>true</threadName>
              <dataType>true</dataType>
              <encoding>false</encoding>
              <assertions>true</assertions>
              <subresults>true</subresults>
              <responseData>false</responseData>
              <samplerData>false</samplerData>
              <xml>false</xml>
              <fieldNames>true</fieldNames>
              <responseHeaders>false</responseHeaders>
              <requestHeaders>false</requestHeaders>
              <responseDataOnError>false</responseDataOnError>
              <saveAssertionResultsFailureMessage>true</saveAssertionResultsFailureMessage>
              <assertionsResultsToSave>0</assertionsResultsToSave>
              <bytes>true</bytes>
              <sentBytes>true</sentBytes>
              <url>true</url>
              <threadCounts>true</threadCounts>
              <idleTime>true</idleTime>
              <connectTime>true</connectTime>
            </value>
          </objProp>
          <stringProp name="filename"></stringProp>
        </ResultCollector>
        <hashTree/>
      </hashTree>
    </hashTree>
  </hashTree>
</jmeterTestPlan>
`;
}

function buildProperties(defaults) {
  return `# HSMS JMeter defaults
users=100
rampUp=20
loops=1
thinkTimeMs=250
maxResponseMs=2000

protocol=${defaults.protocol}
host=${defaults.host}
port=${defaults.port}

username=admin_user
password=change_admin_password
productId=1
supplierId=1
saleId=1
userId=1
`;
}

function main() {
  ensureDir(outputDir);

  const dtoMap = parseDtos();
  const endpoints = parseControllers(dtoMap);
  const defaults = getDefaultConfig();

  const jmx = buildJmx(endpoints, defaults);
  const properties = buildProperties(defaults);

  fs.writeFileSync(jmxFile, jmx);
  fs.writeFileSync(propertiesFile, properties);

  console.log(`Generated ${jmxFile}`);
  console.log(`Generated ${propertiesFile}`);
  console.log(`Included ${endpoints.length} endpoints. Login, sales, reports, and health samplers are enabled by default.`);
}

main();
