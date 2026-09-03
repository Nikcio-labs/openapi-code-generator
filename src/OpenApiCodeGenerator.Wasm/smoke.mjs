import { dotnet } from './dotnet.js';

const spec = `openapi: 3.0.3
info:
  title: Smoke API
  version: 1.0.0
paths:
  /pets:
    get:
      responses:
        '200':
          description: OK
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Pet'
components:
  schemas:
    PetStatus:
      type: string
      enum: [available, pending, sold]
    Pet:
      type: object
      required: [name]
      properties:
        name:
          type: string
        status:
          $ref: '#/components/schemas/PetStatus'
`;

const options = {
  namespace: 'SmokeTests',
  modelPrefix: '',
  includeSchemas: [],
  generateDocComments: true,
  generateFileHeader: true,
  defaultNonNullable: true,
  addDefaultValuesToProperties: true,
  useImmutableArrays: true,
  useImmutableDictionaries: true,
  omitJsonPropertyNameAttributes: false,
  inlinePrimitiveTypeAliases: false,
  emitValidationAttributes: true,
  emitObsoleteAttribute: true,
};

const api = await dotnet.create();
const config = api.getConfig();
console.log('mainAssemblyName:', config.mainAssemblyName);
const exports = await api.getAssemblyExports(config.mainAssemblyName);
const resultJson = exports.OpenApiCodeGenerator.Wasm.WasmInterop.Generate(spec, JSON.stringify(options));
const result = JSON.parse(resultJson);
if (!result.success) {
  console.error('GENERATION FAILED:', result.error);
  process.exit(1);
}
console.log(result.code);
console.log('--- SMOKE TEST PASSED ---');
