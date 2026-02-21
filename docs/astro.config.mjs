// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

// https://astro.build/config
export default defineConfig({
	site: 'https://nikcio-labs.github.io',
  	base: '/openapi-code-generator/',
	integrations: [
		starlight({
			title: 'OpenAPI Code Generator',
			description: 'A fast, opinionated C# code generator that transforms OpenAPI 3.x specifications into modern C# records, enums, and type aliases.',
			head: [
				{
					tag: 'meta',
					attrs: {
						name: 'google-site-verification',
						content: 'aAvqHpzAD8nHnD8OBmwCTNlwzooPa89X_i_b6j3d7J8',
					},
				},
			],
			social: [
				{ icon: 'github', label: 'GitHub', href: 'https://github.com/Nikcio-labs/openapi-code-generator' },
			],
			editLink: {
				baseUrl: 'https://github.com/Nikcio-labs/openapi-code-generator/edit/main/docs/',
			},
			sidebar: [
				{
					label: 'Getting Started',
					items: [
						{ label: 'Introduction', slug: 'getting-started/introduction' },
						{ label: 'Installation', slug: 'getting-started/installation' },
						{ label: 'Quick Start', slug: 'getting-started/quick-start' },
					],
				},
				{
					label: 'Guides',
					items: [
						{ label: 'CLI Usage', slug: 'guides/cli-usage' },
						{ label: 'Configuration', slug: 'guides/configuration' },
					],
				},
				{
					label: 'Reference',
					items: [
						{ label: 'CLI Reference', slug: 'reference/cli' },
						{ label: 'Type Mapping', slug: 'reference/type-mapping' },
					],
				},
			],
		}),
	],
});
