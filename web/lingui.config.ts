import { formatter } from '@lingui/format-po'
import { defineConfig } from '@lingui/cli'

/**
 * One catalog per feature (src/features/<name>/locales), plus app/ and shared/.
 * The runtime globs every locales/*.po, so a new feature catalog needs no wiring.
 */
export default defineConfig({
  sourceLocale: 'en',
  locales: ['en', 'fr'],
  format: formatter({ lineNumbers: false }),
  catalogs: [
    {
      path: '<rootDir>/src/features/{name}/locales/{locale}',
      include: ['<rootDir>/src/features/{name}'],
    },
    { path: '<rootDir>/src/app/locales/{locale}', include: ['<rootDir>/src/app'] },
    { path: '<rootDir>/src/shared/locales/{locale}', include: ['<rootDir>/src/shared'] },
  ],
})
