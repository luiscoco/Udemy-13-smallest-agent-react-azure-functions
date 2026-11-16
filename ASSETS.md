# Assets and references

The course astronaut is the established reference illustration carried forward from the previous local React edition. Its original reference is [luiscoco/Udemy-Claude-Agent-SDK-lesson01-app](https://github.com/luiscoco/Udemy-Claude-Agent-SDK-lesson01-app), commit `832439954c11d4f3623d1b4a311f7dd67389fcc8`. No AI-generated image assets were added.

React styling and educational interaction patterns were adapted from edition 06 within this workspace. The C# SDK streaming/projection pattern was adapted from edition 09, with new Azure Functions boundaries, validation, hosting configuration and independent tests. Source copies were selected individually; environment files, credentials, dependency directories and generated runtime state were not copied.

The UML SVG and editable PlantUML are generated locally from sixteen source definitions by scripts/generate-lesson.mjs. SVG fills, strokes, arrow markers and fonts are explicit to prevent the black-frame and missing-arrow rendering issue. No remote renderer or model-generated code is executed in the frontend.

The backend uses the official Anthropic C# package, as described by the [user-supplied C# SDK documentation](https://platform.claude.com/docs/en/cli-sdks-libraries/sdks/csharp). This is the Messages API client, not the CLI-backed Python/TypeScript Agent SDK. Azure configuration follows the official [isolated-worker guide](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide), [Flex infrastructure guidance](https://learn.microsoft.com/en-us/azure/azure-functions/functions-infrastructure-as-code), and [hosting limits](https://learn.microsoft.com/en-us/azure/azure-functions/functions-scale).
