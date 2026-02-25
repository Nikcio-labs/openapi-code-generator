// @ts-check

import starlight from "@astrojs/starlight";
import { defineConfig } from "astro/config";

// https://astro.build/config
export default defineConfig({
	site: "https://nikcio-labs.github.io",
	base: "/openapi-code-generator/",
	integrations: [
		starlight({
			title: "OpenAPI Code Generator",
			description:
				"A fast, opinionated C# code generator that transforms OpenAPI 3.x specifications into modern C# records, enums, and type aliases.",
			head: [
				{
					tag: "meta",
					attrs: {
						name: "google-site-verification",
						content: "aAvqHpzAD8nHnD8OBmwCTNlwzooPa89X_i_b6j3d7J8",
					},
				},
				{
					tag: "meta",
					attrs: {
						name: "robots",
						content: "index, follow",
					},
				},
				{
					tag: "meta",
					attrs: {
						name: "author",
						content: "Nikcio Labs",
					},
				},
				{
					tag: "meta",
					attrs: {
						name: "keywords",
						content:
							"OpenAPI, code generator, C#, dotnet, .NET, OpenAPI 3, swagger, REST API, code generation, C# records, type generation",
					},
				},
				{
					tag: "meta",
					attrs: {
						property: "og:locale",
						content: "en_US",
					},
				},
				{
					tag: "meta",
					attrs: {
						property: "og:site_name",
						content: "OpenAPI Code Generator",
					},
				},
				{
					tag: "meta",
					attrs: {
						name: "twitter:card",
						content: "summary_large_image",
					},
				},
			],
			social: [
				{
					icon: "github",
					label: "GitHub",
					href: "https://github.com/Nikcio-labs/openapi-code-generator",
				},
			],
			editLink: {
				baseUrl:
					"https://github.com/Nikcio-labs/openapi-code-generator/edit/main/docs/",
			},
			sidebar: [
				{
					label: "Getting Started",
					items: [
						{ label: "Introduction", slug: "getting-started/introduction" },
						{ label: "Installation", slug: "getting-started/installation" },
						{ label: "Quick Start", slug: "getting-started/quick-start" },
					],
				},
				{
					label: "Guides",
					items: [
						{ label: "CLI Usage", slug: "guides/cli-usage" },
						{ label: "Configuration", slug: "guides/configuration" },
					],
				},
				{
					label: "Reference",
					items: [
						{ label: "CLI Reference", slug: "reference/cli" },
						{ label: "Type Mapping", slug: "reference/type-mapping" },
					],
				},
			],
		}),
	],
});
