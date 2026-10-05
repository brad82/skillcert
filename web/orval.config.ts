import { readFileSync } from 'node:fs'
import { defineConfig, type Options } from 'orval'

const spec = './openapi/skillcert.json'

/**
 * API tag → owning feature. Each feature gets its own generated hooks file at
 * src/features/<feature>/api/<camelName>Api.gen.ts. Generation fails on an unmapped tag so new
 * endpoints are always given an owner.
 */
const featureByTag: Record<string, string> = {
  Auth: 'auth',
  CurrentUser: 'current-user',
  MyRecord: 'my-record',
}

const camel = (name: string) => name.replace(/-(\w)/g, (_, c: string) => c.toUpperCase())

const specTags = new Set<string>(
  Object.values(JSON.parse(readFileSync(spec, 'utf8')).paths as Record<string, Record<string, { tags?: string[] }>>)
    .flatMap((operations) => Object.values(operations))
    .flatMap((operation) => operation.tags ?? []),
)
const unmapped = [...specTags].filter((tag) => !(tag in featureByTag))
if (unmapped.length > 0) {
  throw new Error(`orval.config.ts: map these API tags to a feature in featureByTag: ${unmapped.join(', ')}`)
}

const featureClients = Object.fromEntries(
  Object.entries(featureByTag).map(([tag, feature]): [string, Options] => [
    feature,
    {
      input: { target: spec, filters: { mode: 'include', tags: [tag] } },
      output: {
        mode: 'single',
        target: `./src/features/${feature}/api/${camel(feature)}Api.gen.ts`,
        schemas: './src/shared/api/model',
        client: 'react-query',
        httpClient: 'fetch',
        override: {
          mutator: { path: './src/shared/api/client.ts', name: 'apiFetch' },
          fetch: { includeHttpResponseReturnType: false },
        },
      },
    },
  ]),
)

export default defineConfig({
  ...featureClients,
  // Zod schemas for request bodies, used by feature model/ hooks for client-side validation.
  schemas: {
    input: { target: spec },
    output: {
      mode: 'single',
      target: './src/shared/schemas/skillcert.zod.gen.ts',
      client: 'zod',
    },
  },
})
