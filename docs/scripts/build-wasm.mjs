// Builds the browser WebAssembly bundle for the docs playground (the "App" page)
// and copies it into docs/public/playground.
//
// Usage:
//   node scripts/build-wasm.mjs           # reuse existing output if present, otherwise build
//   node scripts/build-wasm.mjs --force   # always republish and re-smoke-test
//   node scripts/build-wasm.mjs --skip-smoke
//   node scripts/build-wasm.mjs --skip-publish  # copy + smoke-test an existing publish output
//
// The bundle is generated output: docs/public/playground/ is gitignored, and CI
// (.github/workflows/docs.yml) builds it during the docs deploy.
//
// On CI the publish runs as a direct workflow step (--skip-publish) instead of
// from inside this script: NuGet restore silently fails (MSB4181 with no logged
// error) when dotnet is spawned as a node/pnpm child process on the runner.
import { execSync, spawnSync } from "node:child_process";
import {
	cpSync,
	existsSync,
	readFileSync,
	rmSync,
	writeFileSync,
} from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const docsDir = path.resolve(scriptDir, "..");
const repoRoot = path.resolve(docsDir, "..");
const wasmProject = path.join(repoRoot, "src", "OpenApiCodeGenerator.Wasm");
const publishDir = path.join(
	wasmProject,
	"bin",
	"Release",
	"net10.0",
	"publish",
	"wwwroot",
	"_framework",
);
const outDir = path.join(docsDir, "public", "playground");

const force = process.argv.includes("--force");
const skipSmoke = process.argv.includes("--skip-smoke");
const skipPublish = process.argv.includes("--skip-publish");

function hasDotnet() {
	try {
		execSync("dotnet --version", { stdio: "pipe" });
		return true;
	} catch {
		return false;
	}
}

const outputExists = existsSync(path.join(outDir, "dotnet.js"));

if (!force && outputExists) {
	console.log(
		"Playground runtime already present in docs/public/playground — reusing it. Use --force to rebuild.",
	);
	process.exit(0);
}

if (skipPublish) {
	if (!existsSync(publishDir)) {
		console.error(
			"error: --skip-publish requires an existing publish output but none was found.\n" +
				`       Run 'dotnet publish "${wasmProject}" -c Release' first.`,
		);
		process.exit(1);
	}
} else {
	if (!hasDotnet()) {
		console.error(
			"error: The .NET SDK is required to build the playground runtime but was not found.\n" +
				"       Install the .NET SDK (see global.json) and re-run, or build on a machine with dotnet available.",
		);
		process.exit(1);
	}

	console.log("Publishing WebAssembly playground runtime…");
	execSync(`dotnet publish "${wasmProject}" -c Release`, {
		stdio: "inherit",
		cwd: repoRoot,
	});
}

rmSync(outDir, { recursive: true, force: true });
cpSync(publishDir, outDir, { recursive: true });

if (!skipSmoke) {
	console.log("Smoke testing the WebAssembly bundle…");
	const smokeSource = readFileSync(
		path.join(wasmProject, "smoke.mjs"),
		"utf8",
	).replace("console.log(result.code);", "");
	const smokeTarget = path.join(outDir, "smoke.mjs");
	try {
		writeFileSync(smokeTarget, smokeSource);
		const smoke = spawnSync("node", ["smoke.mjs"], {
			cwd: outDir,
			stdio: "inherit",
		});
		if (smoke.status !== 0) {
			throw new Error("Playground runtime smoke test failed");
		}
	} finally {
		rmSync(smokeTarget, { force: true });
	}
}

console.log(`Playground runtime written to ${outDir}`);
