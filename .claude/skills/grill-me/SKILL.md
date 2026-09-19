---
name: grill-me
description: Interview and pressure-test a user's request before planning or implementation. Use this skill whenever the user says "grill me", "phỏng vấn tôi", "hỏi ngược", "phản biện yêu cầu", "challenge my assumptions", or asks to clarify a prompt before work begins. Inspect the relevant codebase first, then continue questioning until the expected output, acceptance criteria, scope boundary, non-negotiable constraints, and affected touchpoints are concrete. Do not use for simple factual questions or when the user explicitly requests immediate execution without an interview.
---

# Grill Me

Turn a vague, risky, or assumption-heavy request into an agreed requirement contract before any implementation begins.

Be direct and skeptical without being hostile. The goal is to prevent wasted work, not to win an argument.

## Interaction contract

- Stay in interview mode until the readiness gate passes.
- Use read-only inspection tools during the interview. Do not edit files, run destructive commands, install dependencies, or mutate external state.
- Match the user's language and technical level.
- Ask only questions that could materially change the solution.
- Never ask the user for information that can be discovered safely from the codebase.
- If a user cannot answer, propose a safe default, explain its consequence, and ask them to accept or replace it.
- If the user asks to stop the interview, summarize unresolved gaps and the assumptions required to proceed.
- Start every active interview round with the first question. Keep scouting, status, plans, and context summaries internal until the readiness gate passes.

When a structured user-input tool is available, ask 1–3 related questions per call with 2–3 concrete options. Put the recommended option first and explain its trade-off. Otherwise ask concise direct questions in the response.

## Workflow

### 1. Scout before questioning

Inspect the codebase before asking any clarification:

1. Read applicable repository instructions such as `CLAUDE.md` and local rule files.
2. Identify the project type, runtime, language, framework, and important compatibility constraints.
3. Search for files, symbols, modules, tests, docs, or plans related to the request.
4. Inspect an existing implementation that most closely matches the requested behavior.
5. Check repository status so unrelated user changes are not mistaken for task scope.

Use fast, targeted searches. Do not scan large generated, dependency, cache, or VCS directories unless they are directly relevant.

If no codebase is available, inspect the material supplied by the user. Turn any missing fact that materially affects the solution into a direct question.

Keep scouting findings internal during active interview rounds. When evidence matters, place the minimum necessary fact inside the relevant question or its one-sentence consequence. Clearly distinguish verified facts from inferences without creating a separate pre-question brief.

### 2. Build the requirement contract

Track these five mandatory fields throughout the interview:

| Field | Ready when |
| --- | --- |
| Expected output | The final artifact or behavior is named precisely. |
| Acceptance criteria | Completion can be checked with observable pass/fail conditions. |
| Scope boundary | In-scope and explicitly out-of-scope work are stated. |
| Non-negotiable constraints | Runtime, compatibility, files, conventions, deadlines, and prohibited changes are known. |
| Touchpoints | Affected files, modules, data, APIs, UI, workflows, and users are identified. |

Do not propose solutions while a field contains vague language such as “make it better”, “handle errors”, “improve UX”, or “work automatically”. Ask for an example, input/output pair, edge case, or measurable condition.

### 3. Run focused interview rounds

For each round:

1. Identify the largest unresolved risk or decision.
2. Ask 1–3 questions grounded in codebase evidence.
3. Explain briefly why each answer matters.
4. Challenge assumptions that conflict with the codebase, constraints, cost, schedule, safety, maintainability, or user workflow.
5. Update the five-field contract from the answers.
6. Continue only with gaps that remain unresolved; do not repeat settled questions.

The user-facing response for an active interview round has this exact shape:

1. The first visible content is question 1, or the structured question UI when available.
2. Each question may include concrete options and one short sentence explaining why the choice matters.
3. The response ends after the last question.

Do not place a skill announcement, status update, plan, heading, codebase context, findings list, requirement summary, recommendation, or acceptance criteria before the questions. Present summaries only after the readiness gate passes or when the user asks to stop the interview.

Prefer specific questions:

```text
Should this setting remain in the existing project JSON repository, or move to
Revit Extensible Storage? The second option changes portability and migration
scope, so it cannot be treated as an implementation detail.
```

Avoid abstract questions:

```text
What architecture do you want?
```

If the request combines three or more independent outcomes, flag that the scope is probably too large. Propose a decomposition and ask which outcome is the current priority.

### 4. Pressure-test the request

Test the user's preferred direction against:

- Evidence: does the current codebase support the assumption?
- Necessity: is this required for the stated outcome, or merely attractive?
- Feasibility: can it be delivered within the stated runtime, time, and dependency constraints?
- Compatibility: what existing behavior, data, or users could break?
- Operability: how will failures be diagnosed, rolled back, and supported?
- Verification: can the acceptance criteria actually be tested?

State unrealistic or contradictory assumptions plainly. Cite the evidence and the practical consequence. Offer a narrower alternative instead of merely rejecting the idea.

Do not manufacture objections. Challenge only issues that could materially affect the decision.

### 5. Apply the readiness gate

The interview is ready to conclude only when:

- All five contract fields can each be summarized in one concrete sentence.
- Acceptance criteria include relevant happy paths, failure behavior, and critical edge cases.
- No material contradiction remains between the desired outcome and project constraints.
- Every unresolved item is explicitly accepted as an assumption or deferred out of scope.

If any condition fails, ask another focused round.

### 6. Present 2–3 viable approaches

After the readiness gate passes, present 2–3 genuinely different approaches. Do not create cosmetic variations of the same solution.

Use this comparison:

| Approach | How it works | Advantages | Disadvantages | Risk/effort | Best fit |
| --- | --- | --- | --- | --- | --- |
| A | ... | ... | ... | ... | ... |
| B | ... | ... | ... | ... | ... |
| C | ... | ... | ... | ... | ... |

Recommend one approach. Explain why it best satisfies the agreed contract and name the trade-off the user is accepting.

### 7. Request explicit confirmation

End with:

```markdown
Requirement contract
- Expected output:
- Acceptance criteria:
- In scope:
- Out of scope:
- Non-negotiable constraints:
- Touchpoints:
- Accepted assumptions:

Recommendation
[Chosen approach and decisive reason]

Remaining risks
- [Risk and mitigation]

Confirm this contract or correct the items that are still wrong.
```

Do not treat silence or an ambiguous “looks fine” as approval when a material decision remains open. Once the user explicitly confirms, release the interview gate and follow the user's requested next action or hand the contract to the appropriate planning/implementation skill.

## Quality checks

Before every response, verify:

- Questions are based on inspected evidence.
- The first visible user-facing content is the first question; no preamble or context summary appears before it.
- The current round contains no more than three questions.
- Each question affects scope, design, risk, or acceptance.
- Resolved answers are reflected in the contract.
- No implementation or mutation occurred before approval.
- The tone is candid, concise, and respectful.
