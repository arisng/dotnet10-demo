/*
LADV WebAPI

This is the Learning Adventure FSH Web API project.

Validation Scripts

Glob Pattern Validation

To validate glob patterns in .github/instructions/*.instruction.md files:

1. Install dependencies:

   npm install

2. Run the validation script:

   npm run validate-globs

   or

   node scripts/validate-globs.js

The script will check each glob pattern in the "Applies To" column of the instruction files and report if they are valid or invalid.

Troubleshooting

- If fast-glob is not found, ensure you have run npm install.
- The script assumes Node.js is installed and available in your PATH.
- Glob patterns are validated by attempting to execute them against the project root directory.
*/

const fs = require('fs');
const path = require('path');
const fg = require('fast-glob');

console.log('Validating glob patterns in instruction files...\n');

try {
    const instructionsDir = path.join(__dirname, '..', '.github', 'instructions');
    const pattern = '*.instructions.md';

    const files = fg.sync(pattern, { cwd: instructionsDir });

    if (files.length === 0) {
        console.log('No instruction files found.');
        process.exit(0);
    }

    for (const file of files) {
        const filePath = path.join(instructionsDir, file);
        const content = fs.readFileSync(filePath, 'utf8');

        // Parse the YAML frontmatter
        const lines = content.split('\n');
        if (lines[0].trim() === '---') {
            const endIndex = lines.findIndex((line, i) => i > 0 && line.trim() === '---');
            if (endIndex > 0) {
                const frontmatterLines = lines.slice(1, endIndex);
                const frontmatter = frontmatterLines.join('\n');
                const applyToMatch = frontmatter.match(/applyTo:\s*"([^"]+)"/);
                if (applyToMatch) {
                    const appliesTo = applyToMatch[1];
                    console.log(`Found applyTo in ${file}: ${appliesTo}`);
                    const globs = appliesTo.split(',').map(g => g.trim());
                    for (const glob of globs) {
                        if (glob) {
                            try {
                                // Validate by attempting to use the glob pattern
                                fg.sync(glob, { cwd: path.join(__dirname, '..') });
                                console.log(`✓ Valid: ${glob} (in ${file})`);
                            } catch (e) {
                                console.log(`✗ Invalid: ${glob} (in ${file}) - ${e.message}`);
                            }
                        }
                    }
                }
            }
        }
    }

    console.log('\nValidation complete.');
} catch (error) {
    console.error('Error during validation:', error.message);
    process.exit(1);
}