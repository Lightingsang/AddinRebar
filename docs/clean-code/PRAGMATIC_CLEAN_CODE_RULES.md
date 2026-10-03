# Pragmatic Clean Code — Rule Catalogue (PCC)

> Source: *Pragmatic Clean Code – A practical guide to writing code that lasts*, Krystyna Ślusarczyk (Packt, 2026), read in full from the project's NotebookLM notebook (15 chapters) on 2026-10-03.
> Content is **paraphrased** for internal code review; no code listings are reproduced. Where the book is silent a field reads *(not stated in book)*. Lines tagged *[add-in illustration]*, *(applied)* or *[Revit review hint — not from book]* are the extractor's application of a rule to C#/Revit, not claims of the book.
> **How to use:** this file is the reference (look a rule up by id). The binding, condensed standard for this repository is [REVITADDINAI_CLEAN_CODE_STANDARD.md](REVITADDINAI_CLEAN_CODE_STANDARD.md). Rule ids are stable — append new rules at the end, never renumber.

Rules: **293** across 15 chapters.

## Index

| Id | Ch | Rule |
|---|---|---|
| [PCC-001](#pcc-001) | 1 | Do not treat "it works" as "it is finished" |
| [PCC-002](#pcc-002) | 1 | Apply an explicit, shared definition of done to every change |
| [PCC-003](#pcc-003) | 1 | Refactor continuously; do not let complexity accumulate |
| [PCC-004](#pcc-004) | 1 | Do not buy short-term speed by cutting refactoring, tests, clarity, candour, or rest |
| [PCC-005](#pcc-005) | 1 | Make technical debt visible, record it, and schedule its repayment |
| [PCC-006](#pcc-006) | 1 | Estimate with the full definition of done |
| [PCC-007](#pcc-007) | 1 | Communicate quality risks as concrete outcomes, offer options, and record the decision |
| [PCC-008](#pcc-008) | 1 | Write for the reader, and judge cleanliness by how much it confuses someone reading it |
| [PCC-009](#pcc-009) | 1 | Name methods and parameters by intent, never by truncated words |
| [PCC-010](#pcc-010) | 1 | Make the top-level method read as a sequence of decisions at one level of abstraction |
| [PCC-011](#pcc-011) | 1 | Do not do work on paths that do not need it |
| [PCC-012](#pcc-012) | 1 | Express simple collection questions with intent-revealing operations instead of hand-written search loops |
| [PCC-013](#pcc-013) | 1 | Give each business rule one authoritative implementation |
| [PCC-014](#pcc-014) | 1 | Keep implementations simple: no unnecessary abstraction, cleverness, or branching |
| [PCC-015](#pcc-015) | 1 | Make behavior predictable: no surprising side effects, consistent with names and conventions |
| [PCC-016](#pcc-016) | 1 | Keep behavior verifiable in isolation, without hidden dependencies or heavy setup |
| [PCC-017](#pcc-017) | 1 | Treat AI-generated code as unreviewed code |
| [PCC-018](#pcc-018) | 1 | Remove comments that only restate the code; let names carry the intent |
| [PCC-019](#pcc-019) | 1 | Understand first, then refactor in small, behavior-preserving, verified steps |
| [PCC-020](#pcc-020) | 1 | Treat code that is hard to test as a signal that it needs restructuring |
| [PCC-021](#pcc-021) | 2 | Name variables, parameters, fields, and types with nouns or noun phrases |
| [PCC-022](#pcc-022) | 2 | Name methods with verbs or verb phrases, and disambiguate noun/verb words |
| [PCC-023](#pcc-023) | 2 | Name Booleans and Boolean-returning methods as yes/no questions |
| [PCC-024](#pcc-024) | 2 | Keep Boolean names positive; negate at the point of use |
| [PCC-025](#pcc-025) | 2 | Follow the language's mainstream naming conventions |
| [PCC-026](#pcc-026) | 2 | Write down team naming rules that extend the language conventions, and enforce them with tooling |
| [PCC-027](#pcc-027) | 2 | Judge names from a newcomer's point of view, not the author's |
| [PCC-028](#pcc-028) | 2 | Rename as understanding improves, and leave touched code a little better (Boy Scout rule) |
| [PCC-029](#pcc-029) | 2 | Treat public API names as commitments |
| [PCC-030](#pcc-030) | 2 | Split any construct whose accurate name needs "And", "Or", or "If" |
| [PCC-031](#pcc-031) | 2 | When the best available class name is a vague word like Manager or Handler, inspect the class and split it |
| [PCC-032](#pcc-032) | 2 | Replace noise words such as Data and Info with what the value actually is |
| [PCC-033](#pcc-033) | 2 | Name types after the specific domain concept, not a generic category |
| [PCC-034](#pcc-034) | 2 | Make each name precise enough that it needs no explanatory comment |
| [PCC-035](#pcc-035) | 2 | Keep a type's public surface within what its name promises |
| [PCC-036](#pcc-036) | 2 | Choose a name's length by the information it must carry |
| [PCC-037](#pcc-037) | 2 | Treat an unavoidably long name as a sign of misplaced or excess responsibility |
| [PCC-038](#pcc-038) | 2 | Let test names be long enough to state the scenario and the expected result, using one agreed pattern |
| [PCC-039](#pcc-039) | 2 | Make methods and classes do what their names say, only that, with no hidden behavior (principle of least surprise) |
| [PCC-040](#pcc-040) | 2 | Make variable and parameter names match the value they actually hold |
| [PCC-041](#pcc-041) | 2 | Honour the conventional meaning of Get, Set, Is, Has, and Can (semantic correctness) |
| [PCC-042](#pcc-042) | 2 | Disambiguate homonyms when more than one reading is plausible |
| [PCC-043](#pcc-043) | 2 | Use one word per concept, and reserve different words for different concepts |
| [PCC-044](#pcc-044) | 2 | Match singular or plural to cardinality, and update names when the type changes |
| [PCC-045](#pcc-045) | 2 | Use small connector words so calls read like natural phrases |
| [PCC-046](#pcc-046) | 2 | Use one human language for identifiers, and document it if it is not English |
| [PCC-047](#pcc-047) | 2 | Keep implementation details out of abstraction names |
| [PCC-048](#pcc-048) | 2 | Do not encode types in names (Hungarian notation and type suffixes) |
| [PCC-049](#pcc-049) | 2 | Avoid names that differ only slightly, and name relationships instead of numbering |
| [PCC-050](#pcc-050) | 2 | Avoid abbreviations unless any developer would recognize them instantly |
| [PCC-051](#pcc-051) | 2 | Use surrounding context instead of repeating it, and add context only where a name would be ambiguous |
| [PCC-052](#pcc-052) | 2 | Introduce a type to carry the context that several prefixed names share |
| [PCC-053](#pcc-053) | 2 | Replace comments that label code blocks with extracted, well-named methods |
| [PCC-054](#pcc-054) | 2 | Give loop indices role names when the loops traverse different dimensions |
| [PCC-055](#pcc-055) | 2 | Give a repeated meaningful number a named constant |
| [PCC-056](#pcc-056) | 2 | Review names with a repeatable sequence rather than searching at random for "better words" |
| [PCC-057](#pcc-057) | 3 | Name methods with verbs; name Boolean queries as yes/no questions |
| [PCC-058](#pcc-058) | 3 | Make the whole call site read as an unambiguous sentence |
| [PCC-059](#pcc-059) | 3 | Let the signature's types carry meaning so callers need not read the body |
| [PCC-060](#pcc-060) | 3 | Treat a long parameter list as a design question, not an automatic defect |
| [PCC-061](#pcc-061) | 3 | Reduce parameters by separating responsibilities, not as a goal in itself |
| [PCC-062](#pcc-062) | 3 | Group parameters that form one meaningful concept into a type |
| [PCC-063](#pcc-063) | 3 | Replace behavior-switching Boolean parameters with separately named methods |
| [PCC-064](#pcc-064) | 3 | Do not cut parameters at any cost |
| [PCC-065](#pcc-065) | 3 | Never hide an operation's input in instance state to shorten a signature |
| [PCC-066](#pcc-066) | 3 | Order parameters in a predictable, meaningful sequence |
| [PCC-067](#pcc-067) | 3 | Treat ~20 lines / ~120 characters as a prompt to re-examine a method, not a hard limit |
| [PCC-068](#pcc-068) | 3 | Give each method one coherent task; coordinating steps counts as one task |
| [PCC-069](#pcc-069) | 3 | Split methods that show the "doing too much" signals, even short ones |
| [PCC-070](#pcc-070) | 3 | Keep the operations within a method at one consistent level of abstraction |
| [PCC-071](#pcc-071) | 3 | Extract low-level mechanisms into named methods so they are reused, not repeated |
| [PCC-072](#pcc-072) | 3 | Put behavior on the type whose data it uses exclusively |
| [PCC-073](#pcc-073) | 3 | Order class members top-down: public and high-level first, helpers below |
| [PCC-074](#pcc-074) | 3 | Make prerequisites explicit; never require a hidden call-order ritual |
| [PCC-075](#pcc-075) | 3 | Prefer pure functions: results depend only on explicit inputs |
| [PCC-076](#pcc-076) | 3 | Avoid hidden side effects, especially mutating caller-supplied arguments |
| [PCC-077](#pcc-077) | 3 | Keep impurity at the boundaries; build the core from pure functions |
| [PCC-078](#pcc-078) | 3 | Validate prerequisites early, before the method does other work |
| [PCC-079](#pcc-079) | 3 | Define a set of supported values once, as named constants |
| [PCC-080](#pcc-080) | 3 | Name variables and parameters for what they actually hold |
| [PCC-081](#pcc-081) | 3 | Prefer direct collection operations over hand-written loops |
| [PCC-082](#pcc-082) | 3 | Use lambdas for behavior needed at a single call site; delegates to pass behavior |
| [PCC-083](#pcc-083) | 3 | Refactor a large method as a sequence of focused, behavior-preserving steps |
| [PCC-084](#pcc-084) | 4 | Make the visual layout reveal the program's logical structure |
| [PCC-085](#pcc-085) | 4 | Know where a formatting rule comes from, to decide how strictly to follow it |
| [PCC-086](#pcc-086) | 4 | Apply the same formatting choice everywhere; consistency beats personal preference |
| [PCC-087](#pcc-087) | 4 | Document team conventions beyond the mainstream style, but only the ones that matter |
| [PCC-088](#pcc-088) | 4 | Automate formatting and commit the configuration to the repository |
| [PCC-089](#pcc-089) | 4 | Keep routine formatting out of code review |
| [PCC-090](#pcc-090) | 4 | Indent every nested scope consistently, one level per nesting |
| [PCC-091](#pcc-091) | 4 | Use one whitespace convention (tabs or spaces) across the repository |
| [PCC-092](#pcc-092) | 4 | Always brace block bodies, even single statements |
| [PCC-093](#pcc-093) | 4 | Extract a block whose opening and closing braces don't fit on one screen |
| [PCC-094](#pcc-094) | 4 | Respect a line-length limit, breaking at meaningful points before you reach it |
| [PCC-095](#pcc-095) | 4 | Put comparable Boolean conditions on separate, aligned lines |
| [PCC-096](#pcc-096) | 4 | Break parameter lists all-or-nothing |
| [PCC-097](#pcc-097) | 4 | Break long method chains one call per line, with the dot leading |
| [PCC-098](#pcc-098) | 4 | Break long interpolated or concatenated expressions at logical components |
| [PCC-099](#pcc-099) | 4 | Use single blank lines to separate logical units, not to fragment one construct |
| [PCC-100](#pcc-100) | 4 | Never use more than one consecutive blank line |
| [PCC-101](#pcc-101) | 4 | Don't compact declarations or block bodies onto a single line |
| [PCC-102](#pcc-102) | 4 | Run the automated formatter first, then make the edits that need judgment |
| [PCC-103](#pcc-103) | 4 | Use formatting as a diagnostic: extract blocks that remain too large after formatting |
| [PCC-104](#pcc-104) | 5 | Use SOLID as a vocabulary for reasoning, not as a mechanical checklist |
| [PCC-105](#pcc-105) | 5 | Give each class one coherent responsibility |
| [PCC-106](#pcc-106) | 5 | Resist growing classes by accumulation |
| [PCC-107](#pcc-107) | 5 | Separate independent responsibilities into dedicated, precisely named classes |
| [PCC-108](#pcc-108) | 5 | Test class boundaries with "one reason to change" |
| [PCC-109](#pcc-109) | 5 | Don't measure responsibility by method count; keep operations that serve one purpose together |
| [PCC-110](#pcc-110) | 5 | Recognise orchestrators that delegate detailed work as having one responsibility |
| [PCC-111](#pcc-111) | 5 | Notice who requests changes to a class |
| [PCC-112](#pcc-112) | 5 | Be able to describe the class's responsibility in one brief sentence |
| [PCC-113](#pcc-113) | 5 | Treat vague or compound class names as SRP warning signs |
| [PCC-114](#pcc-114) | 5 | Judge a split by its result; revert splits that leave the new classes tightly coupled |
| [PCC-115](#pcc-115) | 5 | Don't use class size as the SRP criterion |
| [PCC-116](#pcc-116) | 5 | Keep independent axes of variation in separate classes so variants add up instead of multiplying |
| [PCC-117](#pcc-117) | 6 | Program consumers against the contract, not the implementation |
| [PCC-118](#pcc-118) | 6 | Add new behaviour as new implementations when a family of behaviours is expected to grow |
| [PCC-119](#pcc-119) | 6 | Replace type-code conditionals in behaviour with polymorphic implementations |
| [PCC-120](#pcc-120) | 6 | Judge an OCP refactoring by whether existing implementations stay closed, not by "zero edits" |
| [PCC-121](#pcc-121) | 6 | Contain unavoidable concrete-type selection in one small factory |
| [PCC-122](#pcc-122) | 6 | Change the contract openly when a requirement changes what every implementation needs |
| [PCC-123](#pcc-123) | 6 | Abstract only variation that is real now or reasonably likely |
| [PCC-124](#pcc-124) | 6 | Do not trade performance or correctness for formal OCP compliance |
| [PCC-125](#pcc-125) | 6 | Redesign rather than preserve obsolete abstractions after a radical domain change |
| [PCC-126](#pcc-126) | 6 | Fix defects in place; never wrap them in a "corrected" subtype |
| [PCC-127](#pcc-127) | 6 | Use SRP and OCP together, and apply neither mechanically |
| [PCC-128](#pcc-128) | 7 | Treat inheritance as a behavioural contract, not a signature match |
| [PCC-129](#pcc-129) | 7 | Consumers assume only what the base type promises; subtypes keep every promise and convention |
| [PCC-130](#pcc-130) | 7 | Type the consumer by the capability it needs; keep non-universal capabilities off the base type |
| [PCC-131](#pcc-131) | 7 | Distrust "is-a" hierarchies where the subtype must couple members to protect its own invariant |
| [PCC-132](#pcc-132) | 7 | A subtype must not produce invalid results from behaviour the base type guarantees |
| [PCC-133](#pcc-133) | 7 | A subtype must not throw where the base type returns a result |
| [PCC-134](#pcc-134) | 7 | Move capabilities that only some subtypes support into a capability interface |
| [PCC-135](#pcc-135) | 7 | Put logic shared across a capability in a service that accepts the capability interface |
| [PCC-136](#pcc-136) | 7 | Remove concrete-subtype checks from client code |
| [PCC-137](#pcc-137) | 7 | If the special case is legitimate behaviour, move it inside the subtype |
| [PCC-138](#pcc-138) | 7 | If the operation is not valid for a type, stop claiming it: split the capability |
| [PCC-139](#pcc-139) | 7 | Do not weaken postconditions |
| [PCC-140](#pcc-140) | 7 | Do not strengthen preconditions |
| [PCC-141](#pcc-141) | 7 | Don't use inheritance merely to vary a configuration value |
| [PCC-142](#pcc-142) | 8 | Shape each interface around one coherent capability or client role |
| [PCC-143](#pcc-143) | 8 | Use both clients and implementers as evidence that a contract is too broad |
| [PCC-144](#pcc-144) | 8 | Add a new capability as a new interface, not as a new member of a widely used one |
| [PCC-145](#pcc-145) | 8 | Never satisfy an interface with stubs that throw or do nothing meaningful |
| [PCC-146](#pcc-146) | 8 | Split a broad interface along capabilities |
| [PCC-147](#pcc-147) | 8 | Let one class implement several focused interfaces |
| [PCC-148](#pcc-148) | 8 | Make each client depend on the narrowest interface it actually uses |
| [PCC-149](#pcc-149) | 8 | Diagnose with each principle's own question: SRP, LSP and ISP are distinct |
| [PCC-150](#pcc-150) | 8 | Offer a narrower role interface even for a cohesive class when clients use only part of it |
| [PCC-151](#pcc-151) | 8 | Split only when it clarifies dependencies or prevents unsupported behaviour |
| [PCC-152](#pcc-152) | 8 | Before adding a member, check every current and plausible implementer can support it meaningfully |
| [PCC-153](#pcc-153) | 8 | Before changing an interface, check whether unrelated clients or implementers must rebuild or change |
| [PCC-154](#pcc-154) | 9 | Make high-level code depend on abstractions, not on concrete low-level classes |
| [PCC-155](#pcc-155) | 9 | Shape the abstraction around what the client needs, not around who provides it |
| [PCC-156](#pcc-156) | 9 | Keep implementation details out of abstractions |
| [PCC-157](#pcc-157) | 9 | Inject dependencies instead of constructing them inside the consumer |
| [PCC-158](#pcc-158) | 9 | Don't confuse Dependency Inversion with Dependency Injection; check both |
| [PCC-159](#pcc-159) | 9 | Inject a factory abstraction when a dependency needs runtime data |
| [PCC-160](#pcc-160) | 9 | Confine knowledge of concrete types to the creating code, and update it when wiring changes |
| [PCC-161](#pcc-161) | 9 | Hide static low-level APIs behind a thin, logic-free wrapper interface |
| [PCC-162](#pcc-162) | 9 | Don't expose general low-level utilities on a high-level class's interface |
| [PCC-163](#pcc-163) | 9 | Replace type-string branching that picks concrete collaborators with polymorphism |
| [PCC-164](#pcc-164) | 9 | Avoid overrides that reject base-accepted arguments; treat parallel hierarchies as a warning |
| [PCC-165](#pcc-165) | 9 | Move behaviour to the type whose data it uses |
| [PCC-166](#pcc-166) | 9 | Expect SOLID violations to cluster; audit every principle and refactor in stages |
| [PCC-167](#pcc-167) | 10 | Decide static-ness on state use *and* replaceability |
| [PCC-168](#pcc-168) | 10 | Make stateless private helpers static |
| [PCC-169](#pcc-169) | 10 | Don't make public, replaceable behaviour static |
| [PCC-170](#pcc-170) | 10 | Keep polymorphism available: static members cannot implement interfaces or be overridden |
| [PCC-171](#pcc-171) | 10 | Reserve public static for pure, stable operations that never need substitutes |
| [PCC-172](#pcc-172) | 10 | Keep data access, business rules and UI interaction out of public static methods |
| [PCC-173](#pcc-173) | 10 | Wrap static framework APIs you need to control behind an injected interface |
| [PCC-174](#pcc-174) | 10 | Prefer `TimeProvider` for current time in new .NET 8+ code, and keep the wrapping skill |
| [PCC-175](#pcc-175) | 10 | Keep the dependency graph visible on the class surface |
| [PCC-176](#pcc-176) | 10 | Objects must be ready to use as soon as they are constructed |
| [PCC-177](#pcc-177) | 10 | Don't let correctness depend on a hidden initialisation order |
| [PCC-178](#pcc-178) | 10 | Avoid public static fields that expose services or shared state |
| [PCC-179](#pcc-179) | 10 | Prefer instance members so a missing dependency is a compile error |
| [PCC-180](#pcc-180) | 10 | Require collaborators through the constructor, typed as abstractions, at every level |
| [PCC-181](#pcc-181) | 10 | Use the constructor seam to keep unit tests off real infrastructure |
| [PCC-182](#pcc-182) | 11 | Keep data and helper methods private; expose only the intended surface |
| [PCC-183](#pcc-183) | 11 | Recognise and dismantle god classes |
| [PCC-184](#pcc-184) | 11 | Treat class size as a prompt to investigate, not a verdict |
| [PCC-185](#pcc-185) | 11 | Keep a large class intact when its size comes from one cohesive purpose |
| [PCC-186](#pcc-186) | 11 | Don't extract helper classes merely to hide size |
| [PCC-187](#pcc-187) | 11 | Never trade clarity for brevity |
| [PCC-188](#pcc-188) | 11 | Split a class whose honest name needs "And" |
| [PCC-189](#pcc-189) | 11 | Don't hide extra responsibilities behind a narrower name |
| [PCC-190](#pcc-190) | 11 | Scrutinise vague catch-all names |
| [PCC-191](#pcc-191) | 11 | Separate concepts from different abstraction levels that a name combines |
| [PCC-192](#pcc-192) | 11 | Treat a class name that changes with every requirement as an unstable boundary |
| [PCC-193](#pcc-193) | 11 | Find boundaries by clustering members on the data and dependencies they use |
| [PCC-194](#pcc-194) | 11 | Extract behaviour that changes for its own business reasons, then simplify the remaining name |
| [PCC-195](#pcc-195) | 11 | Keep a class at one level of abstraction; push mechanics into lower-level components |
| [PCC-196](#pcc-196) | 11 | Extract only when the new type is a coherent responsibility; avoid class proliferation |
| [PCC-197](#pcc-197) | 11 | Don't extract when it forces private details into the open |
| [PCC-198](#pcc-198) | 11 | Free reusable mechanics trapped inside domain classes |
| [PCC-199](#pcc-199) | 11 | Move a method to the parameter type whose knowledge it encodes |
| [PCC-200](#pcc-200) | 11 | Give an extracted collaborator an abstraction, inject it, and move its exclusive dependencies with it |
| [PCC-201](#pcc-201) | 11 | Keep a refactoring behaviour-preserving |
| [PCC-202](#pcc-202) | 12 | Order class members by what a reader looks for first |
| [PCC-203](#pcc-203) | 12 | Group dependencies and other private fields together near the top |
| [PCC-204](#pcc-204) | 12 | Place callers above the methods they call |
| [PCC-205](#pcc-205) | 12 | Position nested types by their accessibility |
| [PCC-206](#pcc-206) | 12 | A consistently applied team convention beats personal preference |
| [PCC-207](#pcc-207) | 12 | Put one type in one file named after the type |
| [PCC-208](#pcc-208) | 12 | Let tiny, related, stable types share a file |
| [PCC-209](#pcc-209) | 12 | Follow the framework's established folder layout |
| [PCC-210](#pcc-210) | 12 | Organise folders by technical concern or by feature, and apply the choice consistently |
| [PCC-211](#pcc-211) | 12 | Split a crowded or mixed folder into subfolders |
| [PCC-212](#pcc-212) | 12 | Let structure emerge, and reorganise without hesitation |
| [PCC-213](#pcc-213) | 12 | Commit file moves separately from code changes |
| [PCC-214](#pcc-214) | 12 | Keep tests in a separate project that mirrors the production structure |
| [PCC-215](#pcc-215) | 12 | Handle the same situation the same way within a type |
| [PCC-216](#pcc-216) | 12 | Use one name for one kind of operation |
| [PCC-217](#pcc-217) | 12 | Write down conventions that keep settling the same question |
| [PCC-218](#pcc-218) | 12 | Restructure in small, reviewable steps, from members outwards |
| [PCC-219](#pcc-219) | 13 | Expose what the consumer needs, not how the data is stored |
| [PCC-220](#pcc-220) | 13 | Never let a consumer modify another object's internal data |
| [PCC-221](#pcc-221) | 13 | Depend on an abstraction where the collaborator is expected to vary |
| [PCC-222](#pcc-222) | 13 | When a small change cascades, hide the volatile detail behind a stable contract |
| [PCC-223](#pcc-223) | 13 | A class that needs many others set up to be tested carries too much knowledge |
| [PCC-224](#pcc-224) | 13 | Do not inspect concrete subtypes to decide behavior; put the behavior behind the abstraction |
| [PCC-225](#pcc-225) | 13 | Law of Demeter: do not navigate through chains of other objects' internals |
| [PCC-226](#pcc-226) | 13 | Ask for the value you need, not for a container you can dig through |
| [PCC-227](#pcc-227) | 13 | Split a class along the seam where member groups share no state |
| [PCC-228](#pcc-228) | 13 | Use the class name as a cohesion test |
| [PCC-229](#pcc-229) | 13 | Do not destroy cohesion by over-splitting; size alone is not low cohesion |
| [PCC-230](#pcc-230) | 13 | Give each piece of business knowledge one authoritative representation |
| [PCC-231](#pcc-231) | 13 | Build related computations on the authoritative one, not on parallel formulas |
| [PCC-232](#pcc-232) | 13 | Extract mechanical duplication when doing so improves clarity |
| [PCC-233](#pcc-233) | 13 | Keep coincidentally identical rules separate |
| [PCC-234](#pcc-234) | 13 | Tolerate temporary duplication until the shared abstraction reveals itself |
| [PCC-235](#pcc-235) | 13 | BDUF: don't commit to detailed abstractions before the problem is understood |
| [PCC-236](#pcc-236) | 13 | YAGNI: don't build extension points or features before a real requirement needs them |
| [PCC-237](#pcc-237) | 13 | KISS: prefer the simplest design that keeps clarity and required capability |
| [PCC-238](#pcc-238) | 13 | Put a needed-but-undecided infrastructure capability behind an interface |
| [PCC-239](#pcc-239) | 13 | Prefer composition over inheritance for sharing and varying behavior |
| [PCC-240](#pcc-240) | 13 | Recognize the hidden costs of inheritance used for reuse |
| [PCC-241](#pcc-241) | 13 | Model each independent dimension of variation as its own collaborator, not a subclass matrix |
| [PCC-242](#pcc-242) | 13 | Make shared logic testable by composition, not via abstract bases or test-only subclasses |
| [PCC-243](#pcc-243) | 13 | Keep inheritance where it is the right tool |
| [PCC-244](#pcc-244) | 13 | Inject collaborators through the constructor instead of creating concrete ones inside methods |
| [PCC-245](#pcc-245) | 13 | Hide static infrastructure calls behind an interface |
| [PCC-246](#pcc-246) | 13 | Separate presenting results from computing them |
| [PCC-247](#pcc-247) | 13 | Place knowledge in the class that naturally owns it |
| [PCC-248](#pcc-248) | 13 | Make contract return types general so implementations choose the concrete collection |
| [PCC-249](#pcc-249) | 13 | Use one term per concept, and never a term already meaning something else in the code |
| [PCC-250](#pcc-250) | 13 | Do the mechanical hygiene first: format, one type per file, folders, public before private |
| [PCC-251](#pcc-251) | 14 | Fix the name or the structure instead of explaining it in a comment |
| [PCC-252](#pcc-252) | 14 | Treat code as the source of truth; keep comments only where their value pays for their upkeep |
| [PCC-253](#pcc-253) | 14 | Delete "Captain Obvious" comments that restate the code |
| [PCC-254](#pcc-254) | 14 | Replace navigation comments with formatting and extraction |
| [PCC-255](#pcc-255) | 14 | Turn block-description comments into well-named methods |
| [PCC-256](#pcc-256) | 14 | Keep change history in version control, not in file-header changelogs |
| [PCC-257](#pcc-257) | 14 | Delete commented-out code |
| [PCC-258](#pcc-258) | 14 | Write a comment only for context the code cannot carry, and make it precise, complete, brief, and maintainable |
| [PCC-259](#pcc-259) | 14 | Name and reference complex algorithms |
| [PCC-260](#pcc-260) | 14 | Explain every non-trivial regular expression with intent and examples |
| [PCC-261](#pcc-261) | 14 | Document temporary workarounds: why, that they are temporary, when, and who to ask |
| [PCC-262](#pcc-262) | 14 | Use TO-DO comments only for small, short-lived tasks, in one consistent searchable format |
| [PCC-263](#pcc-263) | 14 | Use XML documentation comments for shared/public APIs, describing the contract |
| [PCC-264](#pcc-264) | 14 | Keep required copyright/licence headers as short as policy allows |
| [PCC-265](#pcc-265) | 15 | Make every test runnable automatically, the whole suite from one command |
| [PCC-266](#pcc-266) | 15 | Keep the unit suite fast enough to run after every change |
| [PCC-267](#pcc-267) | 15 | Isolate each test so it fails only when its own class changes |
| [PCC-268](#pcc-268) | 15 | Make tests repeatable and independent of run order |
| [PCC-269](#pcc-269) | 15 | Replace infrastructure collaborators with mocks in unit tests |
| [PCC-270](#pcc-270) | 15 | Verify the call on the collaborator when the behavior is an action |
| [PCC-271](#pcc-271) | 15 | Do not read a green unit suite as proof that the application works |
| [PCC-272](#pcc-272) | 15 | Hold integration and end-to-end tests to the same hygiene |
| [PCC-273](#pcc-273) | 15 | Refactor in small steps and run the suite after each one |
| [PCC-274](#pcc-274) | 15 | Treat hard-to-test code as a design signal and fix the production code |
| [PCC-275](#pcc-275) | 15 | Use test-first (TDD) to settle the API from the caller's side |
| [PCC-276](#pcc-276) | 15 | Treat test code with the same care as production code |
| [PCC-277](#pcc-277) | 15 | Name each test by method, expected result and scenario, using one convention |
| [PCC-278](#pcc-278) | 15 | Keep tests short and move repeated setup into a shared place |
| [PCC-279](#pcc-279) | 15 | Keep arrange, act and assert visibly separate, with one cycle per test |
| [PCC-280](#pcc-280) | 15 | Test one coherent behavior per test |
| [PCC-281](#pcc-281) | 15 | Keep loops, conditionals and exception handling out of test bodies |
| [PCC-282](#pcc-282) | 15 | Replace copy-pasted tests with parameterized tests |
| [PCC-283](#pcc-283) | 15 | Center each test on one logical outcome (not "one assert" dogma) |
| [PCC-284](#pcc-284) | 15 | Do not assert details the behavior does not promise |
| [PCC-285](#pcc-285) | 15 | Do not reach controllable resources through static calls; leave pure statics alone |
| [PCC-286](#pcc-286) | 15 | Depend on an abstraction when a collaborator varies or must be controlled in tests |
| [PCC-287](#pcc-287) | 15 | Mock interfaces, not concrete classes |
| [PCC-288](#pcc-288) | 15 | Inject collaborators through the constructor instead of creating them inside |
| [PCC-289](#pcc-289) | 15 | Keep constructors trivial: assign, allocate, guard, and nothing more |
| [PCC-290](#pcc-290) | 15 | Cover every promised behavior, including guard clauses and regression-prone contracts |
| [PCC-291](#pcc-291) | 15 | Remove redundant test cases |
| [PCC-292](#pcc-292) | 15 | Write the suite so it reads as the class's documentation |
| [PCC-293](#pcc-293) | 15 | When cleaning a class with its tests, strip comment clutter and keep only useful pointers |

## Book-wide framing

- **Premise:** code is read much more than it is written. A solution that gives the right answer today can still be costly and risky to maintain when its intent is unclear, its responsibilities are tangled, or its design resists change. Clean code is code that is easier to understand, test, extend and refactor.
- **Pragmatic, not a rulebook:** the author explicitly rejects presenting clean code as a rigid list of rules. Each practice comes with its reasoning, the problems that appear when it is ignored, and a demonstration of improving existing code through focused refactoring. Reviewers should apply the *reasoning*, not the letter.
- **Case-study method:** most chapters end with a before/after refactoring of realistic messy code, done step by step. Readers are asked to try their own refactoring before reading the analysis.
- **Language scope:** examples are C# (.NET 10 / C# 14, compatible with .NET 8+), but the ideas are presented as portable to Java, C++, JavaScript and Python. The book assumes you can already read basic code and OOP (classes, interfaces, inheritance).
- **Progression:** names → methods → formatting → SOLID (SRP, OCP, LSP, ISP, DIP) → static methods and dependencies → smaller classes → organizing classes/projects → coupling, cohesion, reuse (Law of Demeter, DRY, YAGNI, KISS, composition) → comments → **testability and clean tests (ch15, the last content chapter, which ties the earlier threads together)**.
- **Audience:** junior and mid-level developers building habits, plus experienced developers, tech leads and **code reviewers** who want a practical framework for judging existing code.
- **Author's stance:** clean code is a form of respect for the next reader. Her measure of success is a newcomer reading the code and simply understanding it.
- **AI context:** ch1 (per the overview) argues that AI-assisted coding makes careful review and judgment more necessary, not less.
- **Chapter 15 per the overview:** links code quality with unit testing and testability. It covers unit tests and mocks, how tests support safer change and better design, readable tests, dependency injection, static-method pitfalls, simple constructors, and a final case study refactoring both code and tests.

---

## Chapter 1 — Understanding clean code

### Chapter summary

- **Working code and clean code are different.** Code is clean when people can read, understand, test, and change it without undue effort or risk. Those qualities decide how fast a team ships, how confidently it fixes defects, and how well the system copes as requirements grow.
- **The project story.** The first release is fast and clean. Refactoring is then postponed for deadlines, every change slows down, test coverage erodes, and a late, buggy release follows. Management expects the first-release pace, so the team works overtime and cuts tests and reviews, and things get worse. How fast a team builds version one says little about how fast it can deliver later.
- **Complexity piles up like debris on a construction site.** The clutter is duplicated logic, obsolete abstractions, inconsistent names, tight coupling, and tests that no longer match the design. Only regular refactoring keeps it manageable. Each short-term decision (skip refactoring, reduce tests, accept unclear code, hide concerns, rely on overtime) makes the others worse.
- **Code is a document for its maintainers**, so a feature is finished only when the team's definition of done is met, not when it first produces the right output.
- **Technical debt works like financial debt.** It gives an immediate benefit and creates an obligation with interest. It produces false confidence and a "deadline trap". The professional response is to make it visible, judge its risk, and agree when to repay it. Per LeBlanc's law, "later equals never".
- **Developers are responsible for raising quality risks.** Estimates must cover the full definition of done. Risks should be explained in business outcomes (time, defects, cost, customer impact), with options offered and the decision recorded.
- **Clean code is written for readers.** Measure it by how often a reader is confused. The book's eight characteristics are: readable, clear in intent, simple, focused, low in duplication, predictable, testable, and easy to change. There is no single correct visual form, and refactoring is normal adaptation, not proof of failure.
- **AI assistants speed up the work but do not replace judgment.** Generated code is a proposal, to be reviewed and tested like a colleague's. AI tools tend to add comments that restate the code, miss unwritten business rules, and can silently change behavior. Writing sloppy code in the hope that AI will tidy it just moves the risk downstream.

### Rules

#### PCC-001
**Do not treat "it works" as "it is finished"**
- **Source Chapter:** 1 — Understanding clean code (section: Working code is not finished code)
- **Principle:** Producing the expected result is necessary but not sufficient. Code records the system's behavior and explains it to future maintainers, so it must also be clear and well organized before the work counts as complete.
- **Problem:** The author compares this to a user manual that contains all the right facts in a confusing, repetitive, disorganized form. Nobody would call that manual finished. Code accepted at "it works" piles up unclear structure that slows every later change.
- **Detection Signals:**
  - The PR or commit says it works and the cleanup comes later; `// TODO: refactor`, `// HACK`, or `// temporary` added in the same change.
  - New code passes its tests but has cryptic names, copy-pasted blocks, or one long method.
  - The work is marked complete before the definition-of-done checks run (see PCC-002).
- **Recommended Action:** Before closing the work, re-read it as documentation for the next maintainer. Fix names, structure, and duplication, then check it against the definition of done.
- **Exceptions/Trade-offs:** The book does not ask for perfection. A shortcut can be accepted consciously if it is visible and scheduled (PCC-005). The clean and messy versions may pass the same tests; what differs is how easily the code can be verified, explained, and changed.
- **Related Rules:** PCC-002, PCC-005, PCC-008; ch14 Using comments effectively.
- **Review Question:** Could the next maintainer understand this change without asking its author?

#### PCC-002
**Apply an explicit, shared definition of done to every change**
- **Source Chapter:** 1 — Understanding clean code (section: Working code is not finished code)
- **Principle:** Each project needs a written, team-agreed set of criteria that decides when work is actually finished, not merely functional. A typical set has six items:
  - the agreed acceptance criteria are met;
  - relevant unit, integration, and end-to-end tests pass;
  - the required number of developers has reviewed it;
  - it follows the project's coding and design conventions;
  - required documentation is updated;
  - it is merged into the shared repository and builds successfully.
- **Problem:** Without a definition of done, quality work is treated as optional and dropped whenever a deadline feels tight. The author names the silent disagreement between developers and management about what "done" means as one cause of the failing project.
- **Detection Signals:**
  - A PR merges with failing, skipped (`[Fact(Skip = …)]`, `[Ignore]`), or commented-out tests.
  - Behavior changed but no tests were added or updated.
  - Docs (README, `docs/`, project instruction files) are not updated after a change to a contract or to behavior.
  - The PR was merged with fewer than the required reviewers.
  - The shared branch fails to build after the merge.
- **Recommended Action:** Write the definition of done where the team actually reads it, check every item in review, and block the merge while an item is unmet.
- **Exceptions/Trade-offs:** The exact criteria depend on the organization and the risk of the system. A definition of done does not remove every defect; it makes quality visible and stops long-term stability being traded for short-term appearance.
- **Related Rules:** PCC-001, PCC-004, PCC-006; ch15 Writing testable code and clean tests.
- **Review Question:** Does this change meet every item of the project's definition of done, not just the acceptance criteria?

#### PCC-003
**Refactor continuously; do not let complexity accumulate**
- **Source Chapter:** 1 — Understanding clean code (sections: Why clean code matters; A project that starts well; Complexity requires continuous maintenance)
- **Principle:** A growing codebase collects clutter: duplicated logic, obsolete abstractions, inconsistent names, tightly coupled components, and tests that no longer reflect the design. Complexity does not go away by itself. Regular refactoring is part of the real cost of delivery.
- **Problem:** On a construction site where nobody clears debris, every task becomes slower and less safe. When cleanup keeps being postponed, each release takes longer, coverage becomes harder to keep, and eventually a release arrives late and full of defects.
- **Detection Signals:**
  - The same logic is copied across files or features.
  - Abstractions (interfaces, base classes, adapters) are no longer used, or only fit an old design.
  - The same concept goes by several names (see PCC-043).
  - Tests assert an old design, or are deleted instead of updated.
  - A class grows in every release; "refactor later" comments go untouched for many commits.
- **Recommended Action:** Fold small cleanups into normal work (Boy Scout rule, PCC-028). Record larger refactorings and schedule them (PCC-005). Keep tests in step with the design.
- **Exceptions/Trade-offs:** Fixing every imperfection immediately is not the professional response. A large refactoring right before a release may be impractical, so record it and schedule it instead. Refactoring is not evidence that the original author failed; it is how code adapts as understanding improves.
- **Related Rules:** PCC-004, PCC-005, PCC-013, PCC-028; ch13 Balancing coupling, cohesion, and reuse.
- **Review Question:** Does this change leave the area it touched at least as clean as before, with any skipped cleanup recorded?

#### PCC-004
**Do not buy short-term speed by cutting refactoring, tests, clarity, candour, or rest**
- **Source Chapter:** 1 — Understanding clean code (section: The cost of poor code quality, Table 1.2)
- **Principle:** The book names five tempting decisions. Each gives a visible short-term gain and a compounding long-term cost, and they reinforce one another:
  - skipping refactoring;
  - reducing automated testing;
  - accepting unclear code;
  - hiding quality concerns;
  - relying on overtime.
- **Problem:** The costs, in the same order:
  - future changes need more analysis and rework;
  - defects surface later and regressions are harder to diagnose;
  - other developers take longer to understand the code;
  - management plans against a delivery rate that cannot last;
  - burnout, turnover, and knowledge loss reduce future capacity.
- **Detection Signals:**
  - The number of tests or the coverage drops in a PR; tests are deleted or disabled to get green.
  - Review is bypassed, or "LGTM" is given on a large diff.
  - Unclear names or structure are justified as "no time".
  - The author knows of a risk, but the PR or status report does not mention it.
- **Recommended Action:** Keep tests, review, and cleanup inside the scope of the work. Surface concerns (PCC-007). If a compromise really is unavoidable, record it (PCC-005).
- **Exceptions/Trade-offs:** The book concedes that overtime can meet a deadline once; the cost is in relying on it. A compromise is acceptable when it is visible, understood, and agreed.
- **Related Rules:** PCC-002, PCC-005, PCC-007.
- **Review Question:** Was any test, review, or cleanup skipped to meet a date, and if so, is that recorded?

#### PCC-005
**Make technical debt visible, record it, and schedule its repayment**
- **Source Chapter:** 1 — Understanding clean code (section: Understanding technical debt, LeBlanc's law note)
- **Principle:** Technical debt is the future cost of choosing what is faster or easier now. Like money borrowed, it brings an immediate benefit and an obligation that grows with interest, because more code comes to depend on it. Acknowledge it, judge its risk, and agree when and how it will be paid back. Per LeBlanc's law, unrecorded "later" work tends never to happen.
- **Problem:** Repeatedly skipping cleanup creates false confidence: plans are built on a speed that exists only because work was deferred. This is the deadline trap. The team gets more work because features seemed to arrive fast, has less time to refactor, and the debt grows. When the debt is finally paid, the team looks as if it has slowed down. Debt nobody planned for stays until it becomes a crisis, or quietly becomes the system's normal design.
- **Detection Signals:**
  - `TODO`, `FIXME`, or `HACK` comments with no linked issue or owner.
  - Workaround code ("temporary", "quick fix") with no tracking item.
  - The same "temporary" construct survives several releases.
  - A shortcut is admitted in discussion but missing from the tracker or plan.
- **Recommended Action:** Record the debt when it is found: the area, the risk, and the intended fix. Agree on timing with the people who plan the work, and keep it visible until it is paid.
- **Exceptions/Trade-offs:** Not every imperfection has to be fixed at once, and a large refactoring just before a release may be impractical. The requirement is that the debt be recorded and scheduled, not that it be repaid immediately.
- **Related Rules:** PCC-003, PCC-006, PCC-007.
- **Review Question:** Is every shortcut this change takes recorded somewhere it will be scheduled, with its risk stated?

#### PCC-006
**Estimate with the full definition of done**
- **Source Chapter:** 1 — Understanding clean code (section: Professional responsibility and code quality)
- **Principle:** An estimate must include tests, review, documentation, and integration, not just writing the first version of the code.
- **Problem:** If an estimate covers only the first version, management may promise a date that leaves no room for quality work. That work then looks like extra scope instead of part of finishing the feature.
- **Detection Signals:**
  - A plan or task breakdown lists only implementation steps, with nothing for tests, review, docs, or integration.
  - An estimate is quoted "excluding tests" or "plus cleanup later".
  - Review or testing appears as an optional or stretch item.
- **Recommended Action:** Add each definition-of-done item to the estimate explicitly. Never present quality work as an add-on that can be cut.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-002, PCC-007.
- **Review Question:** Does the estimate or plan for this work include tests, review, documentation, and integration?

#### PCC-007
**Communicate quality risks as concrete outcomes, offer options, and record the decision**
- **Source Chapter:** 1 — Understanding clean code (section: Professional responsibility and code quality, Table 1.4)
- **Principle:** Developers usually understand the technical consequences best, so they are responsible for explaining risk in terms non-technical stakeholders can use. Table 1.4 gives four steps:
  1. Describe the condition: the affected area and what makes changes there hard or risky.
  2. Explain the impact: delivery time, defect risk, support cost, or customer experience.
  3. Present realistic options: a smaller scope, a scheduled refactoring task, or more testing time.
  4. Record the accepted trade-off so the debt stays visible.
- **Problem:** Saying "this module has technical debt" means little to a project manager. A concern that is hidden or vague leads to plans built on an unrealistic delivery rate. Blaming management alone ignores the developers' share of the responsibility.
- **Detection Signals:**
  - A report or PR says "needs refactoring" or "has debt" without saying how delivery, defects, or cost are affected.
  - Risks were raised verbally and never written down.
  - Concerns are raised with no options attached.
  - Accepted trade-offs appear nowhere in plans or decision records.
- **Recommended Action:** Use the four-step structure, and keep the record next to the plan or issue.
- **Exceptions/Trade-offs:** A developer cannot control every business decision. The aim is that decisions are made with accurate information about quality, risk, and real cost, not that the developer always gets their way.
- **Related Rules:** PCC-004, PCC-005, PCC-006.
- **Review Question:** Does the reported risk name the area, its practical impact, the options, and where the decision is recorded?

#### PCC-008
**Write for the reader, and judge cleanliness by how much it confuses someone reading it**
- **Source Chapter:** 1 — Understanding clean code (sections: What makes code clean?; A deliberately informal measurement; Practical characteristics of clean code)
- **Principle:** Code is written for people; the computer runs messy code just as well. The book's tongue-in-cheek measure is confusing moments per minute of reading. The measure is about reading, not writing: developers spend more time understanding existing code than typing new code, and even a solo developer comes back months later without the original context. Readability therefore directly sets the time and effort of maintenance; it is not a matter of taste.
- **Problem:** Frequent surprise or confusion while reading is a sign that the code hides its intent.
- **Detection Signals:**
  - A reviewer has to ask the author what a name, flag, or block means.
  - Understanding one call requires opening several other methods.
  - "Clever" one-liners or dense expressions have to be decoded.
  - Readers in review keep misreading or asking about the same spot.
- **Recommended Action:** Re-read the code as a newcomer would (PCC-027). Rename, extract, and simplify until a reader can follow it without decoding.
- **Exceptions/Trade-offs:** No single visual form is required. Two implementations with different structures can both be high quality. Do not copy one preferred style mechanically; make deliberate choices that reduce the reader's effort and keep the code changeable. Clean code is a skill that keeps developing, and experienced developers still revise their own code.
- **Related Rules:** PCC-014, PCC-015, PCC-027; ch4 Formatting code.
- **Review Question:** Could a reviewer read this code top to bottom without stopping to decode anything?

#### PCC-009
**Name methods and parameters by intent, never by truncated words**
- **Source Chapter:** 1 — Understanding clean code (section: Comparing clean and poorly written code, Table 1.6)
- **Principle:** A method name must say exactly which operation it performs. In the book's example, `Auth` could mean authenticate or authorize, so it becomes `Authorize`. Parameters must name their role: `u` and `ua` become `user` and `action`.
- **Problem:** An ambiguous or abbreviated name forces the reader to translate it mentally, or to read the body to find out what it does.
- **Detection Signals:**
  - Method names that are truncated words (`Auth`, `Calc`, `Proc`, `Upd`, `Chk`).
  - Parameters that are single letters or initialisms (`u`, `ua`, `p`, `el`) outside tiny lambdas, loop counters, or catch blocks.
  - One name that could cover two different operations (authenticate vs authorize, create vs update).
- **Recommended Action:** Rename with the IDE's symbol-aware rename to the full verb phrase and to role-describing parameter names.
- **Exceptions/Trade-offs:** Widely recognized short names (`i`, `ex`, `id`) are acceptable; see PCC-050.
- **Related Rules:** PCC-022, PCC-034, PCC-050.
- **Review Question:** Does each method name say unambiguously which operation it performs, and does each parameter name say its role?

#### PCC-010
**Make the top-level method read as a sequence of decisions at one level of abstraction**
- **Source Chapter:** 1 — Understanding clean code (sections: Comparing clean and poorly written code; Practical characteristics of clean code, "Focused")
- **Principle:** Methods and classes should have one clear responsibility and work at a consistent level of abstraction. In the refactored example, the main method reads as business decisions: check the user exists, then authorize a super user or a user holding the permission. The permission lookup moves into a helper named after the question it answers (`IsUserAuthorizedToPerform`).
- **Problem:** In the original, repository access and matching logic are mixed into the main method, so the reader must follow low-level detail to grasp the high-level decision.
- **Detection Signals:**
  - One method mixes orchestration with low-level data access, loops, or index arithmetic.
  - A method calls a repository, service, or API and then runs a detailed matching loop over the result inline.
  - **(reviewer application, not from book)** An `Execute` or command method that both orchestrates the workflow and walks element-collector results in nested loops.
- **Recommended Action:** Extract Method for each low-level step and name the helper after the question it answers or the action it takes. The calling method should read as a plain sequence of steps.
- **Exceptions/Trade-offs:** (not stated in book). Method design is covered in depth in ch3.
- **Related Rules:** PCC-011, PCC-012, PCC-053; ch3 Writing better methods; ch5 SRP.
- **Review Question:** Does this method read at one level of abstraction, with the details delegated to helpers whose names say what they decide?

#### PCC-011
**Do not do work on paths that do not need it**
- **Source Chapter:** 1 — Understanding clean code (section: Comparing clean and poorly written code, Table 1.6, "Control flow")
- **Principle:** Order the logic so that paths able to finish early do so without unnecessary work. In the example, the original fetched permissions before the super-user check even though a super user never needs them. The refactored version lets the super-user path complete without the lookup.
- **Problem:** Wasted work, and misleading flow: fetching data up front suggests every path needs it.
- **Detection Signals:**
  - A repository, database, API, or collector query runs before a guard that can return without using its result.
  - A local variable is assigned and then never read on an early-return path.
  - An expensive lookup sits above an `if (...) return` that does not depend on it.
- **Recommended Action:** Move the retrieval into the helper that needs it, or below the short-circuiting check, and use `||` or `&&` short-circuiting where natural.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-010, PCC-012.
- **Review Question:** Is every expensive lookup done only on the paths that use its result?

#### PCC-012
**Express simple collection questions with intent-revealing operations instead of hand-written search loops**
- **Source Chapter:** 1 — Understanding clean code (section: Comparing clean and poorly written code, Table 1.6, "Collection check")
- **Principle:** A loop that searches for a match just to answer "does any element satisfy X?" says it in a roundabout way. `Any` states the existence check directly.
- **Problem:** The reader has to simulate the loop to discover that it is only an existence test.
- **Detection Signals:**
  - A `foreach` containing `if (cond) return true;`, followed by `return false;` after the loop.
  - A flag pattern: `bool found = false; foreach … { if (…) { found = true; break; } }`.
  - Manual counting or filtering loops that only feed a yes/no answer.
- **Recommended Action:** Replace the loop with `Any`, or with `All` or a similar operation, so the call names the question being asked.
- **Exceptions/Trade-offs:** (not stated in book). In ch2's case study the author leaves the row and column loops as loops because that refactoring was limited to names, so the rule is not applied blindly outside a change's scope.
- **Related Rules:** PCC-010, PCC-019.
- **Review Question:** Does any hand-written loop answer a question that a single named collection operation would state directly?

#### PCC-013
**Give each business rule one authoritative implementation**
- **Source Chapter:** 1 — Understanding clean code (sections: Practical characteristics of clean code, "Low in duplication"; Complexity requires continuous maintenance)
- **Principle:** A business rule should live in one place rather than in several copies that can drift apart.
- **Problem:** Copies diverge. Duplicated logic is listed among the clutter that slows a growing codebase.
- **Detection Signals:**
  - The same condition, formula, or threshold literal appears in several files.
  - Copy-pasted blocks differ only slightly.
  - The same validation is repeated in a view model and in a service.
  - Fixing a bug requires the same edit in several places.
- **Recommended Action:** Extract the rule into one method or type and have every call site use it.
- **Exceptions/Trade-offs:** (not stated in book). The balance between reuse and coupling is the subject of ch13.
- **Related Rules:** PCC-003, PCC-049; ch13 Balancing coupling, cohesion, and reuse.
- **Review Question:** If this rule changes, is there exactly one place to change it?

#### PCC-014
**Keep implementations simple: no unnecessary abstraction, cleverness, or branching**
- **Source Chapter:** 1 — Understanding clean code (section: Practical characteristics of clean code, "Simple")
- **Principle:** The implementation should avoid abstraction, cleverness, and branching that the problem does not require.
- **Problem:** Each of these adds reading effort without adding value. Obsolete abstractions are also part of the clutter described in PCC-003.
- **Detection Signals:**
  - Interfaces, factories, or base classes with one trivial use and no foreseeable variation.
  - Nested ternaries, bit tricks, or dense LINQ chains that a plain version would state more clearly.
  - Branches that can never be reached, or that exist only "just in case".
  - Generic frameworks built for a single caller.
- **Recommended Action:** Inline unnecessary layers, replace clever constructs with plain code, and remove dead branches.
- **Exceptions/Trade-offs:** The book gives no numeric threshold. Whether an abstraction is "unnecessary" is a judgment call (see PCC-008).
- **Related Rules:** PCC-003, PCC-008; ch6 OCP; ch13 Balancing coupling, cohesion, and reuse.
- **Review Question:** Does every abstraction and branch here earn its place for the current requirement?

#### PCC-015
**Make behavior predictable: no surprising side effects, consistent with names and conventions**
- **Source Chapter:** 1 — Understanding clean code (section: Practical characteristics of clean code, "Clear in intent" and "Predictable")
- **Principle:** The reason for each operation should be evident and not obscured by vague names or surprising side effects. Behavior should match the names, interfaces, and conventions the project uses.
- **Problem:** When behavior diverges from what names and conventions promise, readers can no longer trust the code at a glance.
- **Detection Signals:**
  - Methods whose names suggest a query but which change state.
  - Implementations that break a convention used elsewhere in the project (for example, one repository method throws when every sibling returns null).
  - Hidden mutation of fields or of shared or static state inside an apparently pure helper.
- **Recommended Action:** Rename to reflect the real behavior, or move the side effect where callers expect it (see PCC-039 and PCC-041).
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-039, PCC-041; ch3 Writing better methods.
- **Review Question:** Does each member do only what its name, its interface, and the project's conventions lead a reader to expect?

#### PCC-016
**Keep behavior verifiable in isolation, without hidden dependencies or heavy setup**
- **Source Chapter:** 1 — Understanding clean code (sections: Practical characteristics of clean code, "Testable"; Working with the refactoring case studies)
- **Principle:** It should be possible to verify the code's behavior in isolation, without complex setup or hidden dependencies.
- **Problem:** Code with hidden dependencies cannot be tested cheaply, so defects surface late (PCC-004) and refactoring becomes risky.
- **Detection Signals:**
  - Concrete infrastructure created with `new` inside business logic.
  - Static or global state read from inside calculations.
  - Logic that can only run inside the host application.
  - Tests that need elaborate fixtures or reflection to reach behavior.
  - **(reviewer application, not from book)** Pure geometry or rule logic entangled with `Autodesk.Revit.DB` calls, so it cannot run outside Revit.
- **Recommended Action:** Separate pure logic from its dependencies and pass those dependencies in. The mechanics are covered in ch9 and ch10.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-020; ch9 DIP; ch10 Static methods and dependencies; ch15 Writing testable code and clean tests.
- **Review Question:** Can this logic be unit-tested without the host application or a complicated setup?

#### PCC-017
**Treat AI-generated code as unreviewed code**
- **Source Chapter:** 1 — Understanding clean code (section: Clean code in the age of AI-assisted development, "Review rule" note and Table 1.7)
- **Principle:** Generated code is a proposal, not an approved implementation. Read it, test it, compare its behavior with the original, and hold it to the standard you would apply to a colleague's work. Table 1.7 lists what a developer must still verify:
  - the names match the project's domain language;
  - the new structure preserves behavior and fits the surrounding design;
  - generated tests cover the important cases and assert the correct behavior;
  - explanations match the actual runtime behavior and dependencies;
  - security, performance, architecture, business rules, and effects on the wider codebase are sound.
- **Problem:** AI tools lack the history behind design decisions, business rules that were never written down, and teammates' undocumented reasoning. They can make mistakes or quietly change behavior during a refactoring, and the same prompt can give a different result next time. Writing sloppy code in the expectation that AI will fix it only moves the risk downstream. Clean code remains the developer's responsibility.
- **Detection Signals:**
  - A large AI-produced diff with no before/after behavior check or tests.
  - Identifiers that use generic vocabulary instead of the project's domain terms.
  - Generated tests that assert trivial things or mirror the implementation.
  - A "refactor" that touches business-rule branches or error paths.
  - Explanations in the PR that do not match the code.
- **Recommended Action:** Review the generated code like any colleague's PR: run and extend the tests, compare behavior with the original, and check every item in Table 1.7.
- **Exceptions/Trade-offs:** The book sees AI as valuable for small, distinct tasks, and for larger ones when it has enough system context. Examples are drafting tests, explaining an API, suggesting names, a local refactoring, or a first pass at untangling messy code. The durable skill is judgment, not avoiding the tools.
- **Related Rules:** PCC-018, PCC-019, PCC-043.
- **Review Question:** Has the AI-generated part been read, tested, and checked for behavior preservation and domain fit as strictly as a colleague's code?

#### PCC-018
**Remove comments that only restate the code; let names carry the intent**
- **Source Chapter:** 1 — Understanding clean code (section: Clean code in the age of AI-assisted development)
- **Principle:** AI tools often add comments that repeat what the code already says. Such comments add nothing and go stale as soon as the surrounding code changes. A well-named method communicates the same intent and stays accurate.
- **Problem:** Restating comments are noise, and when the code changes underneath them they become misleading.
- **Detection Signals:**
  - A comment paraphrases the next line (`// loop over permissions`, `// return the result`, `// increment counter`).
  - A per-block narration added in an AI-assisted change.
  - Comments that label a block that could be a method (see PCC-053).
- **Recommended Action:** Delete restating comments. Where a comment labels a coherent block, extract that block into a method named after the label.
- **Exceptions/Trade-offs:** This chapter only criticizes restating comments. When comments are worthwhile is treated in ch14.
- **Related Rules:** PCC-034, PCC-053; ch14 Using comments effectively.
- **Review Question:** Does any comment say only what the code or a method name could already say?

#### PCC-019
**Understand first, then refactor in small, behavior-preserving, verified steps**
- **Source Chapter:** 1 — Understanding clean code (sections: Working with the refactoring case studies; Essential vocabulary, "Refactoring")
- **Principle:** Refactoring means improving quality without changing behavior. The book's workflow has six steps:
  1. Read the original and describe what it does in your own words.
  2. Identify the specific source of difficulty: unclear naming, duplication, mixed responsibilities, or hidden dependencies.
  3. Refactor in small steps, compiling and checking behavior after each meaningful change.
  4. Compare before and after: which reader questions became easier, and which risks shrank.
  5. Consider alternative designs.
  6. Compare with a reference solution.

  Keeping the original version available lets you inspect changes and restore a known starting point.
- **Problem:** (not stated in book as a failure story.) The implied risk is unverified, sweeping changes that alter behavior, which the book warns AI refactorings can do.
- **Detection Signals:**
  - One commit mixes a refactoring with a behavior change.
  - A huge refactor with no intermediate builds or test runs.
  - A refactor whose PR states no concrete problem it solves.
  - No way to compare old and new behavior (original deleted, no tests).
- **Recommended Action:** Split the work into small commits that each build and pass tests. State the difficulty addressed and the reader questions that became easier to answer.
- **Exceptions/Trade-offs:** Clean-code principles guide decisions; they do not require everyone to reach an identical design, so alternatives are legitimate. Keep a refactoring within its scope: ch2's case study deliberately stops at naming even though duplication remains.
- **Related Rules:** PCC-017, PCC-020, PCC-028, PCC-056.
- **Review Question:** Was each refactoring step small, compiled, and checked for unchanged behavior, and does it target a named difficulty?

#### PCC-020
**Treat code that is hard to test as a signal that it needs restructuring**
- **Source Chapter:** 1 — Understanding clean code (section: Working with the refactoring case studies, note)
- **Principle:** Code designed without care is often hard or impossible to test, which the book calls one of the clearest signs that restructuring is needed. You can write tests for the original code before refactoring, but some behavior may be unreachable until the structure changes. Writing tests after a first refactoring round is a reasonable alternative and is what usually happens with legacy code.
- **Problem:** If "hard to test" is accepted as a reason to skip tests, the design problem stays hidden and later changes stay risky.
- **Detection Signals:**
  - Classes with no tests and a "can't be tested" justification.
  - Logic that can only be reached through UI, host events, or private members.
  - Tests that rely on reflection or on giant fixtures.
- **Recommended Action:** Try writing tests against the original code first. If the structure blocks that, do a first, careful refactoring round to expose the logic, then add the tests.
- **Exceptions/Trade-offs:** Writing tests after the first round, rather than before, is explicitly acceptable.
- **Related Rules:** PCC-016, PCC-019; ch15 Writing testable code and clean tests.
- **Review Question:** Has difficulty in testing been treated as a design problem rather than as a reason to skip tests?

### Code smells & anti-patterns catalogue

| Name | Signal | Rule IDs |
|---|---|---|
| "Works, therefore done" | Merged without tests, review, or clean names; `TODO refactor` added in the same change | PCC-001, PCC-002 |
| Skipped definition of done | Skipped or disabled tests, missing reviewers, docs not updated, broken build after merge | PCC-002 |
| Accumulated clutter | Duplicated logic, obsolete abstractions, inconsistent names, stale tests | PCC-003 |
| Short-term speed trade | Deleted tests, bypassed review, "no time" for clear names, risks left unmentioned, reliance on overtime | PCC-004 |
| Invisible technical debt | `TODO`/`HACK`/`temporary` with no tracked item; long-lived workarounds | PCC-005 |
| Implementation-only estimate | Plans or estimates with no test, review, doc, or integration items | PCC-006 |
| Jargon-only risk report | "Needs refactoring" with no impact, options, or record | PCC-007 |
| High confusion per minute | Reviewer must ask what a name or block means | PCC-008 |
| Ambiguous abbreviated name | `Auth`, `Calc`; parameters `u`, `ua` | PCC-009 |
| Mixed abstraction levels | Orchestration and data-access loops in one method | PCC-010 |
| Premature fetch | Data loaded before a guard that returns without it | PCC-011 |
| Manual existence loop | `foreach` + `return true` / `return false`, or found-flag loops | PCC-012 |
| Duplicated business rule | Same condition or constant in several places | PCC-013 |
| Unneeded abstraction or cleverness | Single-use interfaces or factories, nested ternaries | PCC-014 |
| Surprising side effect | Query-named member mutates state | PCC-015 |
| Hidden dependency | `new` of infrastructure, statics, host-only logic in calculations | PCC-016, PCC-020 |
| Unreviewed AI output | Large generated diff with no behavior comparison | PCC-017 |
| Restating comment | Comment paraphrases the next line | PCC-018 |
| Big-bang refactor | One huge commit mixing refactor and behavior change | PCC-019 |

### Refactoring techniques named in the chapter

- Rename variables and functions (part of the book's definition of refactoring).
- Extract smaller classes from larger ones (also in the definition).
- Extract a focused helper method named after the question it answers (`IsUserAuthorizedToPerform`).
- Replace a manual search loop with `Any`.
- Reorder control flow so a short-circuit path (super user) skips unneeded retrieval.
- Refactor in small steps and compile and check behavior after each one.
- Write tests for the original code before refactoring, or after a first refactoring round for legacy code.
- Keep the original and refactored versions side by side to compare them and to restore a known starting point.
- Communicate a quality risk in four steps (condition, impact, options, recorded decision), and record and schedule technical debt.

### Things the author says NOT to do mechanically

- Do not insist that every imperfection be fixed immediately. Make the compromise visible and schedule it instead.
- Do not force a large refactoring just before a release. Record it and schedule it.
- There is no single visual form of clean code. Do not imitate one preferred style; two different structures can both be clean.
- Do not treat refactoring, or a rename, as evidence that the original author failed. It is normal adaptation.
- The definition of done is not universal. Its criteria depend on the organization and the risk of the system.
- Do not reject AI tools, and do not accept their output as-is. They are a proposal that needs human judgment.
- Do not expect every developer to reach an identical design. Principles guide decisions; alternatives are legitimate.
- Tests do not have to come strictly before refactoring. After a first round is acceptable, especially with legacy code.
- Passing the same tests does not make two versions equally clean. Judge by ease of verification, explanation, and change.

---

## Chapter 2 — Meaningful names

### Chapter summary

- **Names make up most of the code a reader sees.** Poor names make correct code expensive to read, and the cost compounds as the codebase grows. Renaming alone can expose intent without changing behavior: `Filter(int l, …)` becomes `GetWordsShorterThan(length, words)`, which can be understood from its signature.
- **Grammar follows the construct.** Variables, fields, parameters, and types get nouns; methods get verbs; Booleans get positive yes/no questions. Names follow the language's casing conventions and the team's written rules, enforced by formatters and analyzers in the build.
- **Poor names come from the curse of knowledge.** The author knows context the code does not hold. Counter it by reading as a newcomer, explaining to a rubber duck, or asking the most junior teammate.
- **Names improve gradually.** Rename freely as understanding grows (Boy Scout rule; IDE rename makes it cheap), but public API names are commitments that need deprecation and versioning.
- **Trouble naming something is a design signal.** "And", "Or", or "If" in a name, vague words like Manager or Handler, and very long names point to constructs doing too much, conditions hidden in names, or behavior on the wrong type. The fixes are to split, separate the question from the action, or move behavior.
- **Names must be honest.** Under the principle of least surprise and semantic correctness, a member does what its name says and nothing more, and Get, Set, Is, Has, and Can carry promises the whole industry relies on.
- **Names must be precise and consistent.** Resolve homonyms, use one word per concept, match plurality to cardinality, use natural phrasing, and stick to one human language. Avoid noise words, leaked implementation details, Hungarian notation, near-identical names, and unfamiliar abbreviations. Use the surrounding context instead of repeating it.
- **The tic-tac-toe case study applies the rules.** `Game.Win(char c)` becomes `TicTacToe.IsWonBy(playerSymbol)` with `_board`, `rowIndex`/`columnIndex`, comment-labelled blocks turned into named methods, and a `BoardSize` constant. The chapter ends with a seven-step naming review workflow.

### Rules

#### PCC-021
**Name variables, parameters, fields, and types with nouns or noun phrases**
- **Source Chapter:** 2 — Meaningful names (section: Naming fundamentals — Naming variables, parameters, and types)
- **Principle:** A name's grammatical form should match what it represents. Data holders and user-defined types (classes, structs, enums, interfaces) are named with nouns. Modifiers may narrow a bare noun (`textFile`, `databaseConnection`, `userToBeUpdated`), but the core stays a noun. Types whose job is to do something rather than carry data conventionally take an "-er" suffix that states that job (`UserAuthorizer`, `PeopleDataReader`).
- **Problem:** The book gives the rule without a separate failure story. The underlying reasoning is that names are most of what is read, so their form should tell the reader what kind of thing they are looking at.
- **Detection Signals:**
  - Fields, locals, or parameters named with verbs (`calculate`, `process`, `load`) or adjectives alone.
  - Classes or enums named with verbs (`Validate`, `ProcessRebar`) or adjectives (`Reusable`).
  - Classes that act but have no doer noun (a service named after data it does not hold).
- **Recommended Action:** Rename to a noun phrase, adding modifiers if the bare noun is too broad; use "-er" for types that perform a job.
- **Exceptions/Trade-offs:** Booleans are the main exception (PCC-023); the book mentions "a few exceptions covered later".
- **Related Rules:** PCC-022, PCC-023, PCC-033.
- **Review Question:** Is every variable, field, parameter, and type named with a noun phrase describing what it is?

#### PCC-022
**Name methods with verbs or verb phrases, and disambiguate noun/verb words**
- **Source Chapter:** 2 — Meaningful names (section: Naming fundamentals — Naming methods)
- **Principle:** A method does something, so a verb belongs at the core of its name (`Read`, `Load`, `DecreaseBalance`, `CountNegativeNumbers`, `RemoveExcessSpaces`). A word like "request" can be both noun and verb. When context does not make clear whether a name is the action or the value, make it specific: `SendRequest` for the method, `request` for the value.
- **Problem:** A noun-named method hides the action. A noun/verb word that context does not resolve leaves the reader unsure whether they are looking at an action or a value.
- **Detection Signals:**
  - Methods named with bare nouns (`Data()`, `Result()`, `Request()`, `Report()`).
  - Noun/verb words used as method names where the call site does not make the role obvious.
- **Recommended Action:** Put the verb in front (`BuildReport`, `SendRequest`).
- **Exceptions/Trade-offs:** If context makes the role clear, noun/verb words are rarely a problem. Boolean-returning methods follow PCC-023 instead.
- **Related Rules:** PCC-009, PCC-021, PCC-023.
- **Review Question:** Does each method name have a verb at its core that states the action?

#### PCC-023
**Name Booleans and Boolean-returning methods as yes/no questions**
- **Source Chapter:** 2 — Meaningful names (sections: Naming Boolean values and methods as questions; case study — Make the Boolean method read like a question)
- **Principle:** Booleans, as variables or as methods that return them, are named neither as nouns nor as verbs but as a question answerable with yes or no (`isShorterThanFiveLetters`, `hasBeenSavedSuccessfully`, `canBeDeleted`). In the case study, `Win(char)` sounds like a command to make someone win. Renamed `IsWonBy(playerSymbol)`, it reads as the question it answers, and so does every call to it.
- **Problem:** A bool method named like a command misleads the reader about whether it acts or asks.
- **Detection Signals:**
  - `bool` fields, properties, or locals whose names do not read as a question (`enabled`, `flag`, `status`, `result`).
  - Methods returning `bool` named as imperatives (`Win`, `Validate`, `Check`, `Exists` used ambiguously) or as nouns.
  - Regex aid: `\bbool\s+(?!Is|Has|Can)[A-Z]\w*\s*\(` for methods. Review the hits; the regex is not a verdict.
- **Recommended Action:** Rename to an `Is…`, `Has…`, or `Can…` question, using connectors where they help (`IsWonBy`, `IsShorterThan`).
- **Exceptions/Trade-offs:** (not stated in book) The book's examples cover is/has/can; it does not discuss other question forms.
- **Related Rules:** PCC-024, PCC-041, PCC-045.
- **Review Question:** Does every Boolean name read as a yes/no question?

#### PCC-024
**Keep Boolean names positive; negate at the point of use**
- **Source Chapter:** 2 — Meaningful names (section: Naming Boolean values and methods as questions)
- **Principle:** `isEnabled` is good and `isNotEnabled` is not. Negation belongs in the code that reads the value, written with the `!` operator, not baked into the name.
- **Problem:** (not stated in book beyond the rule.) A negative name forces double negation (`!IsNotEnabled`) at use sites.
- **Detection Signals:**
  - Identifiers containing `IsNot`, `HasNo`, `CanNot`, `NotX`, or `NoX` as Boolean names.
  - `!IsNot…` double negations at call sites.
- **Recommended Action:** Rename to the positive form and invert every usage (the IDE rename followed by a careful condition flip, verified by tests).
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-023.
- **Review Question:** Is every Boolean name stated positively, with negation expressed by `!` where it is needed?

#### PCC-025
**Follow the language's mainstream naming conventions**
- **Source Chapter:** 2 — Meaningful names (sections: Following language and project conventions, Table 2.2; case study — Identifying the issues)
- **Principle:** C# uses PascalCase for types, methods, properties, and other public members, and camelCase for local variables and parameters. The codebase convention cited in the book prefixes private fields with an underscore, so privacy can be read from the name alone (`_board`). No casing style is better in itself. What matters is that code looks familiar to people who work in that language, so a different language means adopting its conventions, not carrying C# habits along.
- **Problem:** Unconventional names look wrong even when meaningful. Consistent conventions make code look the same whoever wrote it, and conventional forms carry extra information (such as the leading underscore meaning private), which speeds reading and maintenance. In the case study, `gameArray` broke the codebase's underscore rule for private fields.
- **Detection Signals:**
  - camelCase public types or methods; PascalCase locals or parameters.
  - snake_case in C# identifiers.
  - Private fields without the project's underscore prefix, assigned via `this.field = field`.
  - Mixed casing styles within one file.
- **Recommended Action:** Rename to the convention, and encode the rules in tooling (PCC-026).
- **Exceptions/Trade-offs:** Table 2.2 lists SCREAMING_SNAKE_CASE as the usual style for constants across languages, yet the case study names a C# private constant `BoardSize` in PascalCase. The book does not resolve this; follow the project's written convention. Conventions are per language.
- **Related Rules:** PCC-026; ch4 Formatting code.
- **Review Question:** Do all identifiers follow C# casing conventions and the project's field-prefix rule?

#### PCC-026
**Write down team naming rules that extend the language conventions, and enforce them with tooling**
- **Source Chapter:** 2 — Meaningful names (section: Following language and project conventions)
- **Principle:** Teams may add local rules on top of the language conventions, if the rules are written where the team actually reads them and extend the language conventions rather than contradict them. The book's example is always calling the object under test `cut` ("class under test"), so in thousands of tests the subject is recognizable at once. Writing a rule down does not make people follow it, so tooling closes the gap:
  - formatters apply layout rules on save, so layout is not argued about in review;
  - static analyzers flag naming violations while the code is being written;
  - both run in the build pipeline, so the build enforces the convention rather than a reviewer's memory.
- **Problem:** Unwritten rules depend on reviewers remembering them. Rules that contradict the language conventions confuse readers.
- **Detection Signals:**
  - Naming or layout debates recurring in review comments.
  - No naming rules in analyzer or formatter configuration (**reviewer application, not from book:** `.editorconfig` naming rules, analyzers set to error in CI).
  - Team conventions that exist only orally.
  - Tests naming the object under test inconsistently (`sut`, `target`, `service`, `cut` mixed).
- **Recommended Action:** Document the rules and run the formatter and analyzers in the build so a violation fails the build.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-025, PCC-038; ch4 Formatting code; ch15 Writing testable code and clean tests.
- **Review Question:** Are the team's naming rules written down and checked by tooling in the build, rather than by reviewers' memory?

#### PCC-027
**Judge names from a newcomer's point of view, not the author's**
- **Source Chapter:** 2 — Meaningful names (sections: Why developers choose poor names; The curse of knowledge; Using the rubber duck method)
- **Principle:** A name looks fine when written because the author knows the problem, the data, and what each temporary holds, and none of that is in the code. This is the curse of knowledge. Three counters:
  - Read your own code as if you did not write it, picturing a junior developer or someone who joined last week.
  - Explain the code line by line to a rubber duck. Every aside you need (such as "this variable is really the active customer") marks a name that needs work.
  - Better still, ask the most junior teammate to read it, and treat each question as a place where context was missing.

  Do not take those questions personally: such feedback is the fastest way to improve. The best outcome is a newcomer reading the code without asking anything.
- **Problem:** Code that looked clean when written becomes unreadable a month later, even to its author.
- **Detection Signals:**
  - Review threads asking "what does X hold?" or "what does this do?".
  - Explanations in PR descriptions or chat that the code itself does not convey.
  - Names that need a clarifying comment.
  - Variables whose meaning depends on knowledge kept in another file or in the author's head.
- **Recommended Action:** At every point where an explanation was needed, rename until the explanation is unnecessary.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-008, PCC-028, PCC-034.
- **Review Question:** Could a developer who joined last week read these names without asking a single question?

#### PCC-028
**Rename as understanding improves, and leave touched code a little better (Boy Scout rule)**
- **Source Chapter:** 2 — Meaningful names (section: Renaming and the Boy Scout rule, note)
- **Principle:** Good names often arrive gradually: the first name reflects how well the design was understood at that moment. If no name comes, use a temporary one, write and refactor the code, and return to the name once the class does exactly what is intended. Renaming is a normal refactoring step, not an admission of failure. The Boy Scout rule asks you to leave the code you work in a little better than you found it, whether or not you made the mess. For example, fix one vague variable, expand an abbreviation, or rename a class whose responsibility has since become clear. Modern IDEs rename symbols safely across a whole project, so the mechanical cost is small.
- **Problem:** A large cleanup is easy to postpone, and it grows the longer it waits. Small, continuous improvements are easier to sustain.
- **Detection Signals:**
  - Placeholder names surviving to merge (`Temp`, `Foo`, `Class1`, `NewMethod`, `Test2`, `Form1`).
  - Names that describe an earlier design (a class whose name no longer matches its methods).
  - Renames done by text replace rather than a symbol-aware rename (missed or extra hits in the diff).
- **Recommended Action:** Use the IDE's symbol-aware rename and run the relevant tests. Include small name cleanups in the files you touch.
- **Exceptions/Trade-offs:** Public API renames need more care (PCC-029). The rule asks for small improvements, not perfect code.
- **Related Rules:** PCC-003, PCC-019, PCC-029, PCC-056.
- **Review Question:** Does this change leave the names in the code it touched clearer than before, with no placeholder names remaining?

#### PCC-029
**Treat public API names as commitments**
- **Source Chapter:** 2 — Meaningful names (section: Renaming public APIs requires more care)
- **Principle:** Inside a codebase you control, renaming is easy. A public API or shared library carries compatibility obligations. The book imagines `Console.WriteLine` being renamed: every program using it would stop compiling. Changing a public name means deprecation, migration guidance, and usually a new major version, so public names deserve more thought up front.
- **Problem:** Renaming a public name breaks every consumer, and each one needs a mechanical fix before it can build again.
- **Detection Signals:**
  - Renamed or removed `public` or `protected` members in assemblies consumed by other projects.
  - **(reviewer application, not from book)** Renamed properties of serialized or wire DTOs, of names referenced by manifests or configuration, or of shared contract types.
  - A public rename with no `[Obsolete]` forwarding member and no version bump.
- **Recommended Action:** Spend extra thought on public names before publishing. When one must change, deprecate the old name, give migration guidance, and bump the major version.
- **Exceptions/Trade-offs:** Internal names can, and should, be renamed freely (PCC-028).
- **Related Rules:** PCC-028, PCC-047.
- **Review Question:** If a public or shared name changed, is there a deprecation path, migration guidance, and a version bump?

#### PCC-030
**Split any construct whose accurate name needs "And", "Or", or "If"**
- **Source Chapter:** 2 — Meaningful names (sections: When naming is hard, inspect the design; Choosing an appropriate name length)
- **Principle:** When a precise name becomes awkward, the construct may be doing too much, and the naming problem is a design signal. The book's examples:
  - `ActivateAccountAndSendNotification` splits into `ActivateAccount` and `SendNotification`.
  - `EmptyShoppingCartIfSessionInactive` should not hide its condition in the name: the caller asks `IsSessionActive()` and then calls `EmptyShoppingCart()`.
  - `FinalizeOrderAndStartPaymentProcess` splits into `FinalizeOrder` and `StartPaymentProcess`.

  As a rule, a name needing "and", "or", or "if" is worth treating as a refactoring signal.
- **Problem:** The construct combines operations, or hides a condition. Split names are shorter, clearer, and more reusable.
- **Detection Signals:**
  - Regex on member names: `[a-z](And|Or|If|Unless|Then)[A-Z]`.
  - Methods that begin with a guard that silently skips the named action.
  - Method bodies with two clearly separate halves.
- **Recommended Action:** Split the method. Separate the question (a Boolean method) from the action, and move the condition to the call site.
- **Exceptions/Trade-offs:** The book frames this as a signal to inspect, not an automatic split. (not stated in book) Domain terms that happen to contain these words are not addressed.
- **Related Rules:** PCC-031, PCC-037, PCC-039; ch3 Writing better methods; ch5 SRP.
- **Review Question:** Does any name need "And", "Or", or "If" to be accurate, and if so, has the construct been split?

#### PCC-031
**When the best available class name is a vague word like Manager or Handler, inspect the class and split it**
- **Source Chapter:** 2 — Meaningful names (sections: When naming is hard, inspect the design; Avoiding meaningless words)
- **Principle:** A class called `EmailManager` could send messages, create accounts in an email service, validate addresses, or all three. In the example it both sends and creates; splitting it into `EmailSender` and `AccountCreator` makes the names easy. When nothing more specific than Manager, Handler, or a similar vague word fits, the cause is usually a vague class doing several unrelated things, not a weak vocabulary.
- **Problem:** The name tells the reader almost nothing about the job, and it hides mixed responsibilities.
- **Detection Signals:**
  - Class names ending in `Manager`, `Handler`, `Processor`, `Helper`, or `Util(s)` (the book names Manager and Handler and "any other vague word").
  - Public methods of the same class with unrelated verbs (`Send` and `Create`).
- **Recommended Action:** Split the class into focused types, each named for its job, usually with "-er" (PCC-021).
- **Exceptions/Trade-offs:** The book says such words are "sometimes useful", so do not ban them mechanically. **(reviewer application, not from book):** a name that mirrors a framework-defined role, such as a class implementing a host's `IExternalEventHandler`, is a plausible example of the "useful" case.
- **Related Rules:** PCC-032, PCC-033, PCC-035; ch5 SRP; ch11 Designing smaller classes.
- **Review Question:** Can this class's name be made more specific than Manager or Handler, and if not, is that because it does several unrelated things?

#### PCC-032
**Replace noise words such as Data and Info with what the value actually is**
- **Source Chapter:** 2 — Meaningful names (section: Avoiding meaningless words)
- **Principle:** When `order`, `orderData`, and `orderInfo` sit side by side, nothing tells the reader how they differ. If they hold different things, name those things (`itemsInOrder`, `orderingCustomer`, `orderStatus`). If they hold the same thing, two of them should not exist. Replace the generic word with the domain's term.
- **Problem:** The suffix adds characters without adding meaning, and it hides either a real distinction or a duplicate.
- **Detection Signals:**
  - Identifiers ending in `Data`, `Info`, `Details`, or `Object`, especially siblings that differ only by such a suffix in one scope.
- **Recommended Action:** Rename to the specific content, or remove the duplicate variable or type.
- **Exceptions/Trade-offs:** The book says these words are sometimes useful; flag them, do not forbid them.
- **Related Rules:** PCC-031, PCC-049.
- **Review Question:** Does each Data- or Info-suffixed name say something the bare noun does not?

#### PCC-033
**Name types after the specific domain concept, not a generic category**
- **Source Chapter:** 2 — Meaningful names (case study — Identifying the issues; Make the domain visible)
- **Principle:** In the case study, `Game` would fit chess, poker, or hangman equally well. The class is specifically tic-tac-toe, so it becomes `TicTacToe`. A type name should make its domain visible.
- **Problem:** A category name forces the reader into the body to learn what the class actually models. In the case study, everything was in the code but nothing was in the names.
- **Detection Signals:**
  - Classes named with broad category nouns (`Game`, `Item`, `Element`, `Shape`, `Calculator`, `Creator`) whose content models one specific concept.
- **Recommended Action:** Rename to the specific domain term.
- **Exceptions/Trade-offs:** (not stated in book) A type that genuinely represents the general concept keeps the general name. Context can supply the specificity (PCC-051).
- **Related Rules:** PCC-021, PCC-034, PCC-051.
- **Review Question:** Does the type name tell the reader which specific domain concept this is?

#### PCC-034
**Make each name precise enough that it needs no explanatory comment**
- **Source Chapter:** 2 — Meaningful names (sections: Why meaningful names matter; Writing expressive names)
- **Principle:** A good name shows the programmer's intention, precisely, without relying on comments. The book's examples:
  - `Clear`, which removes only trailing whitespace and needs a comment to say so, becomes `RemoveTrailingWhitespaces`, or `TrimTrailingWhitespaces` using familiar string terminology.
  - `GameObject.Transform`, which only changes position, is not wrong but is broader than needed; `Move` makes the effect obvious.
  - `Filter(int l, List<string> items)` becomes `GetWordsShorterThan(length, words)`, after which the signature alone explains the method.
- **Problem:** A rough or too-broad name forces the reader to open the method to find out which kind of operation it is.
- **Detection Signals:**
  - A comment directly above a member explaining what it really does, meaning the name is too broad.
  - Generic verbs (`Process`, `Handle`, `Do`, `Transform`, `Update`, `Clear`) used for a narrower operation.
  - A signature that does not reveal its purpose without reading the body.
- **Recommended Action:** Rename to the narrowest accurate verb or noun, preferring terms already familiar in the domain or library (Trim for strings). Then delete the now-redundant comment.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-018, PCC-027, PCC-053; ch14 Using comments effectively.
- **Review Question:** Would the reader know exactly what this does from the name alone, without a comment and without opening it?

#### PCC-035
**Keep a type's public surface within what its name promises**
- **Source Chapter:** 2 — Meaningful names (section: Writing expressive names)
- **Principle:** A class called `FileWriter` should write to files and offer only that. A public method that checks whether a file exists is out of place. Privately the class can do whatever it needs, and checking existence before writing is reasonable, because that is how it does its job, not what it offers.
- **Problem:** Public members outside the name's promise blur what the type is for.
- **Detection Signals:**
  - Public methods whose verb or topic falls outside the class name (a `*Writer` exposing `Exists` or `Read`; a `*Reader` exposing `Save`).
  - Helpers made `public` only for convenience or for tests.
- **Recommended Action:** Make implementation helpers private, and move unrelated public operations to an appropriately named type.
- **Exceptions/Trade-offs:** Private helpers that serve the named job are fine.
- **Related Rules:** PCC-031, PCC-039; ch8 ISP; ch11 Designing smaller classes.
- **Review Question:** Does every public member fit what the type's name says it offers?

#### PCC-036
**Choose a name's length by the information it must carry**
- **Source Chapter:** 2 — Meaningful names (section: Choosing an appropriate name length)
- **Principle:** A short name saves reading effort only when it keeps the meaning. Between a long, clear name and a short, unclear one, clarity wins. Once the meaning is secure, drop words that add nothing. The progression `onlyPeopleOlderThan18Years` → `peopleOlderThan18Years` → `adultPeople` → `adults` ends at `adults` if the domain defines an adult as older than 18; if it does not, the longer form is safer. Some names must stay longer because the distinction matters: `GetSingleLetterStrings` is more precise than `GetShortStrings` when only strings of length one qualify.
- **Problem:** Filler words make reading slower, and over-shortening loses part of the contract.
- **Detection Signals:**
  - Filler words in identifiers (`only`, `the`, `actual`, `basically`).
  - Shortened names that blur a contract the caller relies on (`GetShortStrings` for exactly one character).
  - Names that assume a domain definition the project does not establish.
- **Recommended Action:** Remove filler, and keep or add the words that carry the distinction.
- **Exceptions/Trade-offs:** The domain decides how much a name must say. Names that are only accurate when very long need PCC-037 or PCC-030; test names follow PCC-038.
- **Related Rules:** PCC-037, PCC-038, PCC-050, PCC-051.
- **Review Question:** Does each word in the name add information, and does the name keep every distinction callers rely on?

#### PCC-037
**Treat an unavoidably long name as a sign of misplaced or excess responsibility**
- **Source Chapter:** 2 — Meaningful names (section: Choosing an appropriate name length)
- **Principle:** If only a very long name fits, the cause is usually the code, not the name. A method may be doing too much (split it, PCC-030), or a variable may hold something too complicated. Responsibility may also be misplaced: `Person.IsHomeAddressValid` is long partly because `Person` carries a job that belongs to `Address`. Moving it to `Address.IsValid()` and calling `person.HomeAddress.IsValid()` makes each part easier to read and puts the responsibility with the data's owner.
- **Problem:** Long names signal behavior living on the wrong type, or an overloaded construct.
- **Detection Signals:**
  - Methods whose names embed one of the class's members plus an operation on it (`Is<Member>Valid`, `Format<Member>`), working mostly on that member's data.
  - Variables whose names have to describe a composite structure.
- **Recommended Action:** Move the method to the type that owns the data and rename it in its new context; or split the method; or introduce a type for the complicated value.
- **Exceptions/Trade-offs:** The book notes the combined expression is not much shorter; the gain is clarity and correct ownership, not character count.
- **Related Rules:** PCC-030, PCC-036, PCC-051, PCC-052; ch11 Designing smaller classes; ch13 Balancing coupling, cohesion, and reuse.
- **Review Question:** Is this name long because the method belongs on another type, or because it does more than one thing?

#### PCC-038
**Let test names be long enough to state the scenario and the expected result, using one agreed pattern**
- **Source Chapter:** 2 — Meaningful names (section: Long test names are often useful)
- **Principle:** Tests are a special case. When a test fails, its name becomes part of the diagnostic output, so extra length is justified when it pins down the failure. The book's pattern names the method under test, the expected outcome, and the condition (`Play_ShallReturnLoss_IfTheUserNeverGuessesTheNumber`). Agree on one pattern and use it consistently.
- **Problem:** A short or vague test name makes a failure uninformative.
- **Detection Signals:**
  - Test names like `Test1`, `TestPlay`, `Works`, or `ShouldWork`.
  - Names missing the expected outcome or the condition.
  - Several naming patterns mixed in one test project.
- **Recommended Action:** Adopt one pattern (team rule, PCC-026) and rename the tests to state the scenario and expected result.
- **Exceptions/Trade-offs:** Do not stretch a name beyond what the scenario needs, but never shorten it at the cost of information needed when it fails.
- **Related Rules:** PCC-026, PCC-036; ch15 Writing testable code and clean tests.
- **Review Question:** On failure, does each test name tell what was tested, under which condition, and what was expected?

#### PCC-039
**Make methods and classes do what their names say, only that, with no hidden behavior (principle of least surprise)**
- **Source Chapter:** 2 — Meaningful names (section: Following the principle of least surprise)
- **Principle:** Code should behave the way a reasonable reader expects. When names are honoured, a method can be read at a high level without opening every call; in the book's `Run` example, reading tickets, processing them, and writing the aggregate tells the whole story, and the reader opens a step only by choice. A member should do what its name says, do only that, do it well, and do nothing else. `SaveUser`, which saves only when an email is present, promises more than it guarantees. `SaveUserIfValid` is more honest but still combines two responsibilities, so the cleaner design asks `IsValid(user)` at the call site and then calls `Save(user)`.
- **Problem:** If `Read` secretly deleted files, or `Write` silently changed unrelated state, high-level reading would become unreliable. Names are useful only when implementations honour them.
- **Detection Signals:**
  - `Read*`, `Get*`, `Load*`, or `Find*` methods that delete, write, commit, or change state.
  - `Write*` or `Save*` methods that also alter unrelated state.
  - Action methods wrapped in a guard that silently skips the action with no else branch or exception.
- **Recommended Action:** Split the question from the action, or move the hidden effect to an honestly named member. As a minimum, rename to describe the real behavior.
- **Exceptions/Trade-offs:** A rename such as `SaveUserIfValid` is acceptable as a more honest step, but the book prefers separating the two responsibilities.
- **Related Rules:** PCC-015, PCC-030, PCC-041; ch3 Writing better methods.
- **Review Question:** Could a reader rely on this member's name without opening it, because it does nothing beyond what the name says?

#### PCC-040
**Make variable and parameter names match the value they actually hold**
- **Source Chapter:** 2 — Meaningful names (section: Following the principle of least surprise)
- **Principle:** A variable holding the first even number should be `firstEvenNumber`, not `firstNumber`. A parameter called `list` that actually holds an array suggests the wrong concrete type, and the mismatch can get worse as the implementation changes.
- **Problem:** The mismatch sends the reader in the wrong direction.
- **Detection Signals:**
  - Names that omit a filter or condition applied to the value (`users` holding only active users).
  - Collection-kind words (`list`, `array`, `dict`, `set`) that do not match the declared type.
- **Recommended Action:** Rename to the actual meaning; drop type words entirely (PCC-048).
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-034, PCC-044, PCC-048.
- **Review Question:** Does each variable name describe exactly what the value is, including any filter applied to it?

#### PCC-041
**Honour the conventional meaning of Get, Set, Is, Has, and Can (semantic correctness)**
- **Source Chapter:** 2 — Meaningful names (section: Maintaining semantic correctness)
- **Principle:** Semantic correctness is least surprise narrowed to a single name. Some words carry industry-wide meaning:
  - Get returns something.
  - Set changes something.
  - Is, Has, and Can answer a yes/no question and leave everything unchanged.

  No compiler enforces these meanings and nobody wrote them down, but every reader assumes them, so using the word makes the promise. If a member does more than its name promises, change the name or move the extra work: a Get that has to load becomes `Load` or `Fetch`, and an expensive computed property becomes a method, because a method call signals work and a property does not.
- **Problem:** A `GetName` that quietly loads data into a field gets called in a loop by someone who assumed it was cheap. An `IsEnabled` that flips a flag while answering causes a bug that takes hours to find, because the culprit line looks like a question and gives no reason to check it.
- **Detection Signals:**
  - `Get*` members that do I/O, run queries, lazy-load into fields, or mutate state.
  - `Is*`, `Has*`, or `Can*` members containing assignments to fields or calls that change state.
  - Property getters with loops, queries, or heavy computation.
  - **(reviewer application, not from book)** A property getter that runs a model-wide element query or forces a document regeneration.
- **Recommended Action:** Rename to `Load…` or `Fetch…`, convert the expensive property to a method, or move the side effect out of the query.
- **Exceptions/Trade-offs:** These are conventions, not compiler rules; consistency is what makes names trustworthy. **(reviewer application, not from book):** PCC-043 standardises on `Get` for repository retrieval, while this rule asks for `Load`/`Fetch` when a Get hides loading work. The book does not draw the exact line; the deciding question is whether the caller can tell from context that work happens.
- **Related Rules:** PCC-015, PCC-023, PCC-039, PCC-043; ch3 Writing better methods.
- **Review Question:** Do Get/Is/Has/Can members only return values, with no hidden loading or state changes, and are expensive computations methods rather than properties?

#### PCC-042
**Disambiguate homonyms when more than one reading is plausible**
- **Source Chapter:** 2 — Meaningful names (sections: Avoiding ambiguity and inconsistency; Avoiding homonyms)
- **Principle:** Some words have several meanings. "Current" can be the present moment or a flow of electricity or liquid; "die" can mean to stop living or the cube rolled in games. Ambiguous words are not automatically bad, because context can settle them: in a board game `die` is clear. When more than one interpretation is plausible, add context: `currentlyChosenDevice`, `electricCurrent`.
- **Problem:** Different readers give different meanings to the same name.
- **Detection Signals:**
  - Bare multi-meaning words as identifiers in code where two domains meet (`current`, `state`, `type`, `section`, `level`, `cover`).
  - **(reviewer application, not from book)** In a Revit add-in, words like `section` (cross-section geometry vs a section view), `type` (element type vs CLR `Type`), or `level` in non-building contexts.
- **Recommended Action:** Add a qualifier that removes the ambiguity.
- **Exceptions/Trade-offs:** When context makes the meaning certain, keep the short word; do not lengthen it mechanically.
- **Related Rules:** PCC-051, PCC-052.
- **Review Question:** Could someone new to this code read any name here with two different meanings?

#### PCC-043
**Use one word per concept, and reserve different words for different concepts**
- **Source Chapter:** 2 — Meaningful names (section: Using synonyms consistently)
- **Principle:** A repository with `GetDateOfBirth`, `FetchCityOfBirth`, and `RetrieveSocialSecurityNumber` uses three words for one operation, so the reader has to check each method to confirm they do the same kind of thing. This creeps in when many developers work on the code over time. Pick one word and use it everywhere; the book picks Get as the shortest and most common. Use different words only when the concepts genuinely differ: if `Add` combines two objects of the same type and `Insert` places an element into a collection, the different words help readers predict different behavior.
- **Problem:** Synonyms make related operations look unrelated and make the code look chaotic.
- **Detection Signals:**
  - Within one type or module, several retrieval verbs (`Get`, `Fetch`, `Retrieve`, `Load`, `Read`), creation verbs (`Create`, `Make`, `Build`, `New`), or deletion verbs (`Delete`, `Remove`, `Erase`) used for the same kind of operation.
  - Calculation pairs like `Calc` and `Compute` used side by side.
- **Recommended Action:** Choose one verb per concept, rename the others, and record the vocabulary in the team's naming rules.
- **Exceptions/Trade-offs:** Different words are right when the semantics differ; make sure the difference is real and consistent.
- **Related Rules:** PCC-026, PCC-041, PCC-046, PCC-049.
- **Review Question:** Does each concept use exactly one word across this type or module, with different words marking genuinely different behavior?

#### PCC-044
**Match singular or plural to cardinality, and update names when the type changes**
- **Source Chapter:** 2 — Meaningful names (section: Matching singular and plural forms)
- **Principle:** A name should show whether a value is one item or a collection. `inactiveUserId` holding an array should be `inactiveUserIds`, unless the type is one composite identifier, in which case `inactiveUserId` of a `CompositeId` is fine. When a refactoring changes one item to many, update both the method name and the parameter names (`NormalizeVector(vector)` → `NormalizeVectors(vectors)`). Plurality mistakes typically appear after refactoring, when a type changes and the name is left behind.
- **Problem:** A wrong number in the name misleads the reader about the value's shape.
- **Detection Signals:**
  - Singular names declared as arrays, `List<T>`, or `IEnumerable<T>`; plural names on scalars.
  - Methods with singular names whose signature takes or returns a collection.
  - Diffs that change a parameter's type from `T` to a collection of `T` but keep the name.
- **Recommended Action:** Rename the variable, the parameters, and the method together, in the same change as the type change.
- **Exceptions/Trade-offs:** A single composite domain value keeps a singular name.
- **Related Rules:** PCC-040, PCC-048, PCC-056.
- **Review Question:** Does every name's number match whether it holds one value or a collection?

#### PCC-045
**Use small connector words so calls read like natural phrases**
- **Source Chapter:** 2 — Meaningful names (section: Using natural language in code)
- **Principle:** Small words like "than", "by", "to", and "with" turn identifiers into phrases a reader would naturally say: `vector.IsShorter(other)` becomes `IsShorterThan(other)`, and `point.Transform(1, 2)` becomes `point.MoveBy(1, 2)`. The change is tiny, but the call reads like a sentence.
- **Problem:** Without the connector, the argument's role at the call site is unclear.
- **Detection Signals:**
  - Comparison or relation methods whose argument role is ambiguous at the call site (`IsShorter(x)`, `Offset(a, b)`, `Distance(p)`).
- **Recommended Action:** Add the connector word that states the argument's role.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-023, PCC-034.
- **Review Question:** Does each call read like a phrase that makes each argument's role clear?

#### PCC-046
**Use one human language for identifiers, and document it if it is not English**
- **Source Chapter:** 2 — Meaningful names (section: Choosing a language for identifiers)
- **Principle:** Many international projects use English identifiers because ecosystems, APIs, documentation, and communities do. Some organizations or regulated projects require another language. Consistency matters more than the choice: document a non-English choice and apply it everywhere.
- **Problem:** Switching languages for equivalent concepts makes readers translate both the logic and the vocabulary at once.
- **Detection Signals:**
  - The same domain concept named in two languages in different classes or methods.
  - Mixed-language compound identifiers.
- **Recommended Action:** Pick the language, write the choice down, and rename the outliers.
- **Exceptions/Trade-offs:** A required non-English vocabulary is fine when documented and applied consistently.
- **Related Rules:** PCC-026, PCC-043.
- **Review Question:** Are identifiers written in one documented language, without any concept named in two languages?

#### PCC-047
**Keep implementation details out of abstraction names**
- **Source Chapter:** 2 — Meaningful names (section: Avoiding overly specific names)
- **Principle:** A name can be too specific as well as too vague. It should describe the abstraction callers care about, not an implementation detail that may change. An interface method `ReadFromSqlDatabase` leaks storage details; callers only need to know that `Read` returns people. Implementing classes can be specific, because they name a concrete source: `PeopleFromSqlDatabaseReader` and `PeopleFromExcelFileReader` both implement `IPeopleReader.Read`.
- **Problem:** If the storage moves, a name mentioning SQL becomes false, and every caller reads a lie until someone renames it.
- **Detection Signals:**
  - Interface, abstract, or public API members containing technology words (`Sql`, `Excel`, `Json`, `Xml`, `Http`, `Com`, `File`, `Registry`).
  - Abstractions named after their only current implementation.
- **Recommended Action:** Give the abstraction a generic name and put the specifics in the implementing type names.
- **Exceptions/Trade-offs:** Concrete implementations should be specific.
- **Related Rules:** PCC-029, PCC-048; ch8 ISP; ch9 DIP.
- **Review Question:** Do interface and public abstraction names describe what callers get rather than how it is implemented?

#### PCC-048
**Do not encode types in names (Hungarian notation and type suffixes)**
- **Source Chapter:** 2 — Meaningful names (sections: Avoiding Hungarian notation and type-based names; case study — Identifying the issues)
- **Principle:** Prefixes like `intAge` and `strLastName` made sense when finding a variable's type meant hunting for its declaration. Modern IDEs show types on hover, so the prefix buys nothing and costs characters at every use. It also becomes false when the type changes: `intCount` turns into a `long`, `numbersList` into an array. Prefer domain names (`age`, `lastName`, `numbers`). In the case study, `gameArray` names its type rather than its content, and `c` most likely abbreviates `char` instead of describing a player's symbol.
- **Problem:** The type in the name becomes a lie as soon as the implementation changes, the same failure as PCC-047.
- **Detection Signals:**
  - Regex on identifiers: `\b(int|str|dbl|flt|bool|b|n|s|lst|arr|obj|dict)[A-Z]\w*`.
  - Suffixes like `List`, `Array`, `Dict`, `String`, or `Int` on variables.
  - Single-letter names that abbreviate the type (`c` for a char, `s` for a string).
- **Recommended Action:** Rename to the role the value plays in the domain.
- **Exceptions/Trade-offs:** (not stated in book) The book's own examples keep the `I` prefix on interfaces (`IPeopleReader`) and endorse the underscore prefix for private fields (PCC-025). Those are language and project conventions, not type encoding.
- **Related Rules:** PCC-040, PCC-044, PCC-047, PCC-050.
- **Review Question:** Does any name encode its data type instead of its role?

#### PCC-049
**Avoid names that differ only slightly, and name relationships instead of numbering**
- **Source Chapter:** 2 — Meaningful names (section: Avoiding confusingly similar names)
- **Principle:** Names that differ by one letter or a small grammatical change (`UserDataStorage` and `UsersDataStorage`; `ItemsCount` and `CountOfItems` in one scope) make readers remember distinctions the names do not explain. Numbered parameters (`parameter1`, `parameter2`) carry less meaning than names that state a relationship, such as `source` and `target`.
- **Problem:** Readers confuse the two, or have to remember an arbitrary difference.
- **Detection Signals:**
  - Identifiers within an edit distance of 1–2 in the same scope or namespace.
  - Classes differing only in singular versus plural.
  - Numbered names (`param1`, `value2`, `point1`/`point2`) where a relationship exists.
- **Recommended Action:** If the behavior is duplicated, consolidate it. If it differs, rename both to expose the difference, or use relationship names.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-013, PCC-032, PCC-043.
- **Review Question:** Are there two names in scope a reader could confuse, or names told apart only by a number?

#### PCC-050
**Avoid abbreviations unless any developer would recognize them instantly**
- **Source Chapter:** 2 — Meaningful names (section: Using abbreviations carefully, Table 2.3)
- **Principle:** Abbreviations save characters but usually cost reading time. `addr` is less immediate than `address`, and `prod` could mean product or production. The size of the source is no reason to shorten identifiers. In lambdas, a descriptive parameter (`address => …` rather than `a => …`) reads better and can be searched for. Single letters are effectively unsearchable.
- **Problem:** Readers have to interpret the abbreviation, it may have several readings, and it is hard to find with search.
- **Detection Signals:**
  - Truncated or vowel-dropped identifiers (`addr`, `prod`, `cnt`, `mgr`, `calc`, `tmp`, `res`).
  - Single-letter lambda parameters in non-trivial lambdas.
  - Single-letter variables outside loop indices, catch blocks, or simple math.
  - `i` used for a collection element rather than an index (as in the book's `foreach (var i in items)`).
  - Project-internal acronyms.
- **Recommended Action:** Expand to the full word, and name lambda parameters after the element they represent.
- **Exceptions/Trade-offs:** Some abbreviations are so universal that expanding them would look odd: `ex` in a catch block, `i`, `j`, `k` as short loop indices, `id`, and `a`/`b` in simple math. The test is not length but whether a reader from outside the project would recognize the name without asking. **(reviewer application, not from book):** judge ecosystem-standard names such as the Revit API samples' `doc` and `uidoc` by the same outside-reader test.
- **Related Rules:** PCC-009, PCC-048, PCC-054.
- **Review Question:** Would a developer from outside the project read every abbreviation here without asking?

#### PCC-051
**Use surrounding context instead of repeating it, and add context only where a name would be ambiguous**
- **Source Chapter:** 2 — Meaningful names (section: Using context effectively, "Context rule" note)
- **Principle:** A name never stands alone: the containing type, namespace, project, and call site already supply information. Repeating that context makes names longer without making them clearer:
  - `TextFileWriter` needs `Write`, not `WriteToTextFile`.
  - In a project entirely about email, the `Email` prefix on `SearchEngine`, `Notifier`, `Sender`, and `Validator` adds noise and makes searching harder, unless other domain objects would make the shorter names ambiguous.
  - In the other direction, a bare `state` has no context (process state or a US state?), so `addressState` is better.

  The rule: do not repeat what the reader can reliably infer, and add context only where leaving it out would cause ambiguity.
- **Problem:** Repetition costs reading effort; missing context costs comprehension.
- **Detection Signals:**
  - Member names that repeat the class name (`TextFileWriter.WriteToTextFile`, `Address.AddressState`, `Rebar.RebarDiameter`).
  - Every class in a namespace sharing the same domain prefix.
  - Conversely, bare generic names (`state`, `data`, `value`, `item`) in wide scopes.
- **Recommended Action:** Remove redundant prefixes, and add qualifiers where a name is ambiguous.
- **Exceptions/Trade-offs:** Keep a prefix when other domain objects in the project would otherwise make the name ambiguous.
- **Related Rules:** PCC-033, PCC-036, PCC-042, PCC-052.
- **Review Question:** Does each name avoid repeating what its type or namespace already says, while remaining unambiguous?

#### PCC-052
**Introduce a type to carry the context that several prefixed names share**
- **Source Chapter:** 2 — Meaningful names (section: Using context effectively)
- **Principle:** Instead of `addressNumber`, `addressCountry`, and `addressState` side by side, bundle them into an `Address` struct with `Number`, `State`, and `Country` fields. The type supplies the "address" part, so `Address.State` can only mean a state like California, never the state of a process.
- **Problem:** Repeated prefixes are a sign of a missing type, and they lengthen every name.
- **Detection Signals:**
  - Groups of fields, locals, or parameters sharing a prefix (`addressX`, `startX`/`startY`/`startZ`, `beamWidth`/`beamHeight`/`beamLength`).
  - Parameter lists containing several same-prefix parameters.
- **Recommended Action:** Introduce a struct, record, or class for the group and drop the prefixes.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-037, PCC-051; ch3 Writing better methods (parameter design).
- **Review Question:** Are there several names sharing a prefix that should become one type?

#### PCC-053
**Replace comments that label code blocks with extracted, well-named methods**
- **Source Chapter:** 2 — Meaningful names (case study — Identifying the issues; Replace explanatory comments with focused method names)
- **Principle:** In the case study, comments separated the row, column, and diagonal checks. Each was a coherent operation, and a comment is a weaker label than a method name. After extraction into `IsAnyRowFilledWith`, `IsAnyColumnFilledWith`, and `IsAnyDiagonalFilledWith`, the top-level method reads as a direct statement of the winning condition.
- **Problem:** The comments were doing the naming, and they cannot be composed, reused, or kept accurate the way a method can.
- **Detection Signals:**
  - `// Check …`, `// Step N`, or `// --- section ---` comments splitting one method into blocks.
  - Long methods structured by section comments.
- **Recommended Action:** Extract each labelled block into a method named after what it checks or does, then compose them in the top-level method.
- **Exceptions/Trade-offs:** Which comments should stay is covered in ch14. The case study stops after naming even though the row and column methods are still structurally similar.
- **Related Rules:** PCC-010, PCC-018, PCC-034; ch14 Using comments effectively.
- **Review Question:** Is any block labelled by a comment that could instead be a method with that name?

#### PCC-054
**Give loop indices role names when the loops traverse different dimensions**
- **Source Chapter:** 2 — Meaningful names (case study — Replace generic loop indices when domain names help)
- **Principle:** A plain `i` is fine in a short loop. When one loop walks rows and another walks columns, though, the code should say which: `rowIndex` and `columnIndex`.
- **Problem:** Generic indices hide which dimension a loop traverses, which invites swapped-index bugs and slows reading.
- **Detection Signals:**
  - `i` and `j` used to index 2-D arrays (`grid[i, j]`) or parallel collections.
  - Nested loops whose indices have different meanings.
  - The same `i` reused across loops over different collections.
- **Recommended Action:** Rename each index to the role it plays.
- **Exceptions/Trade-offs:** A short, simple loop keeps `i` (Table 2.3).
- **Related Rules:** PCC-050.
- **Review Question:** Where several indices are in play, does each index name say what it walks?

#### PCC-055
**Give a repeated meaningful number a named constant**
- **Source Chapter:** 2 — Meaningful names (case study — Replace explanatory comments with focused method names)
- **Principle:** The case study extracted the board size into a constant (`BoardSize`), so its meaning no longer has to be inferred from a bare number scattered through the code.
- **Problem:** A bare number repeated in several places hides its meaning, and every copy has to be changed together.
- **Detection Signals:**
  - The same numeric literal with a domain meaning repeated in loop bounds or conditions.
  - Unit or size factors written inline.
- **Recommended Action:** Extract a constant whose name states the meaning, and use it everywhere the quantity appears.
- **Exceptions/Trade-offs:** The book applies this to the repeated, meaningful quantity (the board size). Its final code still indexes cells with literal positions 0, 1, 2, so it does not convert every literal mechanically.
- **Related Rules:** PCC-013, PCC-034.
- **Review Question:** Is every repeated number whose meaning a reader would otherwise have to infer given a name?

#### PCC-056
**Review names with a repeatable sequence rather than searching at random for "better words"**
- **Source Chapter:** 2 — Meaningful names (section: A practical naming review workflow)
- **Principle:** The book gives a seven-step sequence:
  1. Read the signature before the implementation and note what you expect from the name alone.
  2. Check that the name's form fits the construct: nouns for things, verbs for actions, questions for Boolean answers.
  3. Look for ambiguity, inconsistent synonyms, abbreviations, type information, and words that carry no domain meaning.
  4. Check singular and plural, and confirm the name still matches the current type after refactoring.
  5. Use the class, namespace, and project context to remove repeated information without creating ambiguity.
  6. If the only accurate name is extremely long or covers several responsibilities, inspect the design before forcing a shorter name.
  7. Rename with IDE tooling and run the relevant tests to confirm behavior is unchanged.
- **Problem:** Ad hoc renaming misses whole categories of naming problems, and unverified renames risk breaking behavior.
- **Detection Signals:**
  - Renames done by text replace.
  - Renames not followed by a test run.
  - Reviews that comment on names without checking form, consistency, plurality, or context.
- **Recommended Action:** Apply the seven steps to each new or changed name in a PR, and finish with a symbol-aware rename and a test run.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-021–PCC-055, PCC-019.
- **Review Question:** Was each new or changed name checked through the full sequence, ending with a tool-driven rename and a test run?

### Code smells & anti-patterns catalogue

| Name | Signal | Rule IDs |
|---|---|---|
| Wrong grammatical form | Verb-named variable or class; noun-named method | PCC-021, PCC-022 |
| Command-named Boolean | `bool Win(...)`, `bool Validate(...)` | PCC-023 |
| Negative Boolean | `IsNotEnabled`, `!IsNot…` | PCC-024 |
| Convention violation | camelCase public members; private field with no `_`; snake_case in C# | PCC-025 |
| Unenforced convention | Naming debates in review; no analyzer or formatter in the build | PCC-026 |
| Curse-of-knowledge name | Reviewers ask what a name holds; meaning lives only in the author's head | PCC-027 |
| Placeholder or stale name | `Temp`, `Class1`, names reflecting an old design | PCC-028 |
| Careless public rename | Renamed public or shared member with no deprecation or version bump | PCC-029 |
| Conjunction or condition name | `…And…`, `…Or…`, `…If…` in member names | PCC-030 |
| Vague role class | `*Manager`, `*Handler` with unrelated public methods | PCC-031 |
| Noise-word variable | `orderData`, `orderInfo` beside `order` | PCC-032 |
| Generic category type | `Game` for a tic-tac-toe class | PCC-033 |
| Over-broad name needing a comment | `Clear` with a comment saying it trims the end | PCC-034 |
| Off-name public surface | `FileWriter.FileExists()` public | PCC-035 |
| Filler or contract-losing length | `onlyPeopleOlderThan18Years`; `GetShortStrings` for exactly one character | PCC-036 |
| Misplaced-responsibility long name | `Person.IsHomeAddressValid` | PCC-037 |
| Uninformative test name | `Test1`, `Works`; mixed test-name patterns | PCC-038 |
| Hidden behavior | `Read` that deletes; `SaveUser` that may not save | PCC-039 |
| Misleading variable | `firstNumber` for the first even number; `list` holding an array | PCC-040 |
| Broken verb semantics | `Get` that loads or mutates; `Is` that flips a flag; expensive property | PCC-041 |
| Homonym | Bare `current` or `state` in a mixed domain | PCC-042 |
| Synonym chaos | `Get`/`Fetch`/`Retrieve` for one operation | PCC-043 |
| Plurality mismatch | `inactiveUserId` holding an array; singular method taking a list | PCC-044 |
| Phrase-less relation | `IsShorter(x)`, `Transform(1, 2)` | PCC-045 |
| Mixed human languages | One concept named in two languages | PCC-046 |
| Leaky abstraction name | `IPeopleReader.ReadFromSqlDatabase` | PCC-047 |
| Hungarian or type name | `intAge`, `strLastName`, `numbersList`, `gameArray`, `c` for a char | PCC-048 |
| Near-duplicate names | `UserDataStorage` vs `UsersDataStorage`; `param1`/`param2` | PCC-049 |
| Cryptic abbreviation | `addr`, `prod`, `res`, `l`; single-letter lambda parameters | PCC-050 |
| Repeated context | `TextFileWriter.WriteToTextFile`; an `Email*` prefix on every class | PCC-051 |
| Missing context | Bare `state`, `data`, `value` in a wide scope | PCC-051, PCC-042 |
| Prefix cluster | `addressNumber`/`addressCountry`/`addressState` | PCC-052 |
| Comment-labelled blocks | `// Check rows` inside one long method | PCC-053 |
| Anonymous multi-dimension indices | `board[i, j]` with nested `i`/`j` | PCC-054 |
| Repeated bare number | `3` scattered as a board size | PCC-055 |

### Refactoring techniques named in the chapter

- **Rename** with the IDE's symbol-aware rename across the project, then run the relevant tests.
- **Temporary name first:** write and refactor the code, then return to name it once its job is clear.
- **Boy Scout cleanup:** small naming fixes in whatever code you touch.
- **Split method** when a name needs "And" or "Or" (`ActivateAccount` + `SendNotification`; `FinalizeOrder` + `StartPaymentProcess`).
- **Separate the question from the action:** move the condition out of a name or body into a Boolean method checked at the call site (`IsSessionActive` / `EmptyShoppingCart`; `IsValid` / `Save`).
- **Split class** with a vague name into focused "-er" types (`EmailManager` → `EmailSender` + `AccountCreator`).
- **Move method to the data owner** (`Person.IsHomeAddressValid` → `Address.IsValid`).
- **Rename Get to Load/Fetch** when it does hidden work; **convert an expensive property to a method**.
- **Consolidate near-duplicate types**, or rename them to expose the difference.
- **Introduce a type to carry context** (prefixed variables → `Address` struct).
- **Generalize the abstraction's name** and keep implementation specifics in concrete class names (`IPeopleReader.Read` with `PeopleFromSqlDatabaseReader`).
- **Extract methods to replace block-label comments** (`IsAnyRowFilledWith` and the others).
- **Extract a constant** for a repeated meaningful number (`BoardSize`).
- **Public rename path:** deprecate, give migration guidance, release a new major version.
- **Readability probes:** read as a newcomer, rubber-duck walkthrough, junior-teammate reading.
- **Tooling:** formatter on save plus static naming analyzers, both in the build pipeline.
- **One agreed test-naming pattern.**

### Things the author says NOT to do mechanically

- Ambiguous words are not automatically bad; if the context settles them (`die` in a board game), keep them.
- Manager, Handler, Data, and Info are "sometimes useful". Treat them as a prompt to inspect, not a banned list.
- Short conventional names are fine: `ex` in a catch, `i`/`j`/`k` as short loop indices, `id`, `a`/`b` in simple math. The test is recognizability, not length.
- Shorter is not always better. Clarity beats brevity, and a longer name is safer when the domain does not define the shorter term.
- Test names may be long. Do not shorten them at the cost of diagnostic information.
- Do not strip context where leaving it out creates ambiguity. A project-wide prefix stays if other domain objects would otherwise be confused.
- "And", "Or", or "If" in a name is a refactoring signal to inspect, not an automatic split.
- Rename freely inside your own code, but not public or shared APIs without deprecation and versioning.
- No casing style is better in itself. Do not carry C# habits into another language.
- Team naming rules must extend the language's conventions, not override them.
- Non-English identifiers are acceptable when the choice is documented and applied consistently.
- A singular name is correct for a single composite value (`CompositeId`), even when it wraps several numbers.
- Private helpers outside a type's named purpose are fine (a `FileWriter` privately checking existence). Only the public surface must match the name.
- `SaveUserIfValid` is an honest interim rename, but separating the question from the action is the cleaner design.
- Stay within the refactoring's scope. The case study leaves the similar row and column methods for later because naming was the goal.
- Use `i` in a short, single-dimension loop; role names are for loops where the dimension matters.
- Do not convert every literal into a constant. The case study names the repeated board size but keeps literal cell positions.

---

## Chapter 3 — Writing better methods

### Chapter summary
- Methods are where clean-code decay shows first: they gradually pick up parameters, hidden dependencies, branches, file I/O, validation and state changes. They still work, but each extra job makes them harder to read, test and change safely.
- The remedy is explicitly **not** chasing a target method length or parameter count. Instead: signatures that state what the method needs and returns, bodies focused on one task, dependencies kept visible.
- A signature is a contract. Name, types and the arguments at the call site together should tell the caller what happens without opening the body.
- Parameter lists grow mostly for two reasons: the method does several jobs, or loose values actually form one concept. Fix the cause by splitting or grouping. Never hide inputs in fields, never invent meaningless wrappers, and accept that some operations really need several independent values.
- A Boolean flag that switches the method between jobs signals two responsibilities. A Boolean that is plain data for one decision is fine.
- A method should do one coherent task (orchestration counts) and call operations roughly one abstraction level below itself. Table 3.1 lists the signals of a method doing too much.
- At class level: order members top-down, make prerequisites explicit (no hidden setup rituals), prefer pure functions in the core and keep impurity at the boundaries.
- Case study: a large method is improved through a series of focused, behavior-preserving steps. The improvements reinforce each other; they are not isolated rules.

### Rules

#### PCC-057
**Name methods with verbs; name Boolean queries as yes/no questions**
- **Source Chapter:** 3 — Writing better methods (section: Naming methods; case study "Clarifying the signature")
- **Principle:** Methods perform actions, so build their names around a verb (Add, Search, Load, Modify). Methods that return a bool are the exception: their names read as questions (IsLoaded, CanBeDisabled, HasBeenModified).
- **Problem:** A noun-like method name names a thing, not an action. In the case study, `IdFinder` (a method) and the catch-all class `FileManager` told the reader nothing, so every line had to be read to learn what the code did.
- **Detection Signals:**
  - Method names that are nouns or agent nouns (`IdFinder()`, `Validation()`, `Processor()`).
  - bool-returning methods without a question form (`bool Check(...)`, `bool Status()`).
  - Containing classes named with words that could mean anything (`*Manager`).
- **Recommended Action:** Rename the method to a verb phrase that states the operation (example: `IdFinder` → `CheckIfIdExistsInFile`). Rename bool returns to Is/Can/Has-style questions. Rename a vague containing class after its purpose (example: `FileManager` → `IdExistenceChecker`).
- **Exceptions/Trade-offs:** Boolean-returning methods are the stated exception to "verb first". No other exceptions are stated.
- **Related Rules:** PCC-058, PCC-080, PCC-113; ch2 Meaningful names.
- **Review Question:** Does every method name state an action, or ask a yes/no question when it returns bool?

#### PCC-058
**Make the whole call site read as an unambiguous sentence**
- **Source Chapter:** 3 — Writing better methods (section: Designing clear method signatures)
- **Principle:** A method name is never read alone. At the call site it appears with its arguments, and the combination must settle what happens.
- **Problem:** Example: `_fileWriter.Save("someString")` could write that string as content or write to a file of that name. The reader has to open the implementation to find out.
- **Detection Signals:**
  - Generic verbs (`Save`, `Process`, `Handle`, `Do`, `Run`) taking a string or literal whose role is unclear.
  - Call sites where you cannot tell whether the argument is content, a target, or an identifier.
  - Write operations whose name does not say whether existing content is kept or replaced.
- **Recommended Action:** Rename so the verb reveals the argument's role and the effect. Book example: `AppendText(content)` when content is kept, `ReplaceContentWith(content)` when it is overwritten, `WriteTo(fileName)` when the argument is a file name.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-057, PCC-059, PCC-063.
- **Review Question:** Can you tell what this call does, and what each argument means, without opening the method?

#### PCC-059
**Let the signature's types carry meaning so callers need not read the body**
- **Source Chapter:** 3 — Writing better methods (section: Designing clear method signatures)
- **Principle:** The signature (name, return type, parameters, modifiers) is the contract visible to callers. It should make requirements and result clear on its own. Types carry meaning, and the name should add to them rather than do all the work alone. Example: a method taking a list plus a predicate over the same item type and returning a same-type list can reasonably be guessed to be a filter, even without its name.
- **Problem:** An opaque contract forces the caller into the implementation to learn what the method needs and gives back.
- **Detection Signals:**
  - Primitive or `string` parameters standing in for richer concepts (path parts, coordinates, identities).
  - `object` / untyped returns, or results that can only be understood by reading the body.
- **Recommended Action:** Introduce types that express the concept (see PCC-062: `Point`, `FileIdentity`). Choose return types that state what comes back.
- **Exceptions/Trade-offs:** The book admits a guess from types alone may be wrong. Types support the name; they do not replace it.
- **Related Rules:** PCC-058, PCC-062.
- **Review Question:** Could a caller reasonably guess what this method does from its signature alone?

#### PCC-060
**Treat a long parameter list as a design question, not an automatic defect**
- **Source Chapter:** 3 — Writing better methods (section: Choosing the right number of parameters)
- **Principle:** Every parameter is something the caller must remember and supply correctly, and a dimension tests must cover.
  - Zero parameters is the easiest case.
  - One is still simple but already admits wrong input (example: `First()` cannot be misused, `ElementAt(-1)` can).
  - A long list should trigger a question: does it expose several responsibilities, or groups of data that should be modelled?
- **Problem:** As lists grow, callers juggle more information and tests must cover more combinations. Example: a `ConnectToDatabase` taking seven separate configuration values.
- **Detection Signals:**
  - Signatures with many primitive parameters, especially several adjacent `string`s.
  - Parameters that share a prefix or suffix (`databaseName`, `databaseServerName`, `databaseClusterName`).
  - Constructors or methods that take long lists of configuration values.
- **Recommended Action:** First diagnose the cause. Split the method (PCC-061) or group a real concept (PCC-062). If the values are truly independent, leave them (PCC-064).
- **Exceptions/Trade-offs:** The book gives **no hard maximum**. A long list does not automatically mean the method is wrong. A five-parameter connection method is called "enough to be a problem" in itself, but the fix offered is meaningful grouping, not a numeric cut.
- **Related Rules:** PCC-061, PCC-062, PCC-064, PCC-065, PCC-096.
- **Review Question:** Does this parameter list reveal several jobs or an unmodelled concept, rather than genuinely independent inputs?

#### PCC-061
**Reduce parameters by separating responsibilities, not as a goal in itself**
- **Source Chapter:** 3 — Writing better methods (section: Splitting methods to reduce parameters)
- **Principle:** A method often collects parameters because it performs more than one task. Splitting it gives each part fewer inputs. The smaller count is a consequence of the split, not its purpose.
- **Problem:** Example: `SaveGame(gameData, fileName, extension)` both built the final file name and wrote the data. The name-building logic was also unavailable to other code, such as a matching load method.
- **Detection Signals:**
  - Some parameters are used only in the first lines, to compute an intermediate value (a path, key or name) that the rest of the body consumes.
  - Disjoint subsets of parameters are used by disjoint parts of the body.
- **Recommended Action:** Extract the sub-computation into its own method (example: `BuildFileName(name, extension)`). Have the caller compose the two calls, and reuse the extracted method wherever the same computation is needed.
- **Exceptions/Trade-offs:** Only the stated caveat: reduce parameters as a side effect of a better split, never for its own sake.
- **Related Rules:** PCC-060, PCC-068, PCC-069, PCC-105.
- **Review Question:** Do some parameters only feed a separable sub-task that could be its own method?

#### PCC-062
**Group parameters that form one meaningful concept into a type**
- **Source Chapter:** 3 — Writing better methods (section: Grouping related parameters; case study "Clarifying the signature")
- **Principle:** Values that naturally belong together become one type, so the signature describes the concept the way the domain does. Book examples:
  - `x, y` → `Point` (a circle is a centre plus a radius).
  - Database name, server and cluster → `DatabaseIdentity`.
  - User name and password → `UserCredentials`.
  - Directory, name and extension → `FileIdentity`.
- **Problem:** Loose values hide that they describe one thing. The new type is also reusable anywhere else that currently passes the same loose values.
- **Detection Signals:**
  - Adjacent parameters with similar names or a shared prefix. The book calls similar names a hint that they describe one thing.
  - Coordinate pairs or triples passed loose (`double x, double y`).
  - The same cluster of parameters repeated across several methods.
  - Parameters concatenated into one value inside the body (path building).
- **Recommended Action:** Introduce a struct or record for the concept (the book uses `struct` and `record`) and replace the loose parameters. Then look for behavior that uses only that data and move it onto the new type (PCC-072).
- **Exceptions/Trade-offs:** Only worthwhile when the group is a real concept. A wrapper made only to lower the count masks the problem instead of solving it.
- **Related Rules:** PCC-059, PCC-060, PCC-064, PCC-072; ch13 Balancing coupling, cohesion, and reuse.
- **Review Question:** Do these parameters together name one domain concept that deserves its own type?

#### PCC-063
**Replace behavior-switching Boolean parameters with separately named methods**
- **Source Chapter:** 3 — Writing better methods (section: Using Boolean parameters carefully; Note "Review question")
- **Principle:** A Boolean parameter that makes a method do one thing when true and another when false hides two responsibilities behind an unreadable call.
- **Problem:** Example: `CalculateDistance(p1, p2, true)` versus `CalculateDistance(p1, p2, false)`. Nothing at the call site explains the difference. Inside, the flag chooses kilometres or miles, so the method both computes a distance and converts units.
- **Detection Signals:**
  - Literal `true`/`false` arguments at call sites.
  - A bool parameter driving an `if` whose branches do different work or return results of a different kind.
  - Parameter names such as `shouldBeAsX`, `useX`, `xMode`.
- **Recommended Action:** Keep the method doing the first job only and put the variant in its name (example: `CalculateDistanceInKilometers`). Expose the other job as its own method (example: `UnitConverter.KilometersToMiles`). Alternatively, make the choice explicit in the API.
- **Exceptions/Trade-offs:** Not every bool parameter is a problem. A bool that is simply data feeding one decision is legitimate (example: `IsAuthorized(userName, isAdmin)`). The test is whether `true` and `false` cause different responsibilities.
- **Related Rules:** PCC-058, PCC-068, PCC-069, PCC-105.
- **Review Question:** Do `true` and `false` make this method perform different responsibilities?

#### PCC-064
**Do not cut parameters at any cost**
- **Source Chapter:** 3 — Writing better methods (section: Avoiding parameter-count anti-patterns)
- **Principle:** Some operations need several independent values, and no grouping changes that. Leave them explicit.
- **Problem:** Bundling independent values into another object adds indirection without making the concept clearer.
- **Detection Signals (signs of over-correction):**
  - A new wrapper type with no domain meaning, introduced in the same change that shortened a signature.
  - A wrapper used at exactly one call site.
  - A type that merely mirrors one method's argument list.
- **Recommended Action:** Keep independent parameters as they are. Book examples:
  - The `DateTime` constructor (year to second).
  - `AreEqualWithinTolerance(a, b, tolerance)`.
  - A static `CalculateTotalPrice(quantity, unitPrice, tax)`.
- **Exceptions/Trade-offs:** This rule is itself the counterweight to PCC-060–PCC-062.
- **Related Rules:** PCC-060, PCC-062, PCC-065.
- **Review Question:** Was a parameter removed or bundled only to lower the count, without making the concept any clearer?

#### PCC-065
**Never hide an operation's input in instance state to shorten a signature**
- **Source Chapter:** 3 — Writing better methods (section: Avoiding parameter-count anti-patterns)
- **Principle:** Fields hold state that belongs to the object and must persist between calls (a list's elements; connection details a connector reuses across Connect and Disconnect). An input to one particular operation belongs in that method's signature.
- **Problem:** Example: moving `tax` into a field of `OrderPriceCalculator` does not remove the dependency, it hides it.
  - The call no longer shows what the calculation is based on.
  - A different rate needs a new object.
  - Isolated testing becomes harder than before.
- **Detection Signals:**
  - A field set in the constructor or a setter and read by exactly one method.
  - Properties assigned immediately before a call (`calc.Tax = x; calc.Compute(...)`).
  - A formerly static, parameter-driven helper turned into an instance method that reads a field.
  - Private mutable fields used to pass values between methods.
- **Recommended Action:** Ask: would the object still make sense holding this value after the method has returned? If not, make it a parameter again.
- **Exceptions/Trade-offs:** Stable configuration the object genuinely needs across calls belongs in the constructor (PCC-074).
- **Related Rules:** PCC-064, PCC-074, PCC-075; ch10 Static methods and dependencies.
- **Review Question:** Would this object still have a reason to hold this field after the one method that uses it returns?

#### PCC-066
**Order parameters in a predictable, meaningful sequence**
- **Source Chapter:** 3 — Writing better methods (section: Ordering parameters logically; case study "Clarifying the signature")
- **Principle:** Parameter order should follow a pattern callers can predict:
  - From larger units to smaller (year → second; cluster → server → database).
  - The natural reading order of the concept (directory → name → extension).
  - The order implied by the method name (`IsPersonPresentInGroup(person, group)`, `CheckIfIdExistsInFile(id, fileIdentity)`).
- **Problem:** A mismatched order makes the signature harder to read and easier to call wrongly, especially with several parameters of the same type.
- **Detection Signals:**
  - Adjacent same-type parameters in an order that contradicts their natural hierarchy.
  - Parameters ordered opposite to the nouns in the method name.
  - The same parameters ordered differently across sibling methods.
- **Recommended Action:** Reorder the parameters. If the out-of-order parameters form one concept, group them (PCC-062).
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-062, PCC-096.
- **Review Question:** Does the parameter order match the method name and the natural order of the concept?

#### PCC-067
**Treat ~20 lines / ~120 characters as a prompt to re-examine a method, not a hard limit**
- **Source Chapter:** 3 — Writing better methods (section: Keeping methods small and focused)
- **Principle:** Long methods force the reader to keep more state, branches and intermediate values in mind. Use the screen as a heuristic: past roughly 20 lines, or past 120 characters wide, a method deserves a second look.
- **Problem:** Very large methods are harder to understand and test, and more likely to combine unrelated responsibilities. Small methods are easier to name and reuse.
- **Detection Signals:**
  - Method bodies longer than about 20 lines.
  - Lines wider than about 120 characters.
- **Recommended Action:**
  - Long lines usually just need re-breaking, with no design change (PCC-094).
  - For a long method, ask whether every part serves the same task at the same conceptual level. If not, split it (PCC-069, PCC-070).
- **Exceptions/Trade-offs:** The numbers are warning thresholds, not limits. The book's own counter-examples:
  - A cohesive 25-line method can be clearer than five artificial helpers.
  - A six-line method can still mix unrelated responsibilities.
- **Related Rules:** PCC-068, PCC-069, PCC-093, PCC-094, PCC-115.
- **Review Question:** For a long method: does every part contribute to one task at one conceptual level?

#### PCC-068
**Give each method one coherent task; coordinating steps counts as one task**
- **Source Chapter:** 3 — Writing better methods (section: Giving each method one task)
- **Principle:** A focused method does one coherent job. That job can be:
  - A small computation (squaring a number, testing evenness).
  - Orchestration: `ReadUsersData` connects, reads JSON and deserializes by calling lower-level operations instead of implementing each step itself.
- **Problem:** A method that both coordinates and implements every detail no longer has one purpose, which makes it hard to name, test and reuse. The chapter's opening argument is that each extra responsibility makes change less safe.
- **Detection Signals:**
  - A method body that mixes coordinating calls with inline implementation of those same steps.
  - Inability to state the method's job in one short phrase.
- **Recommended Action:** Let the high-level method delegate each substep to an operation one level of abstraction lower.
- **Exceptions/Trade-offs:** A method that coordinates several calls is **not** a violation. Orchestration is one task (mirrors PCC-110 for classes).
- **Related Rules:** PCC-069, PCC-070, PCC-110.
- **Review Question:** Can this method's job be stated as one action, with substeps delegated rather than implemented inline?

#### PCC-069
**Split methods that show the "doing too much" signals, even short ones**
- **Source Chapter:** 3 — Writing better methods (section: Recognizing methods that do too much; Table 3.1)
- **Principle:** Table 3.1 lists the signals that a method may need splitting:
  - Mixed abstraction levels.
  - Large size.
  - Difficulty naming it.
  - AND, OR or IF needed in the only accurate name.
  - Commented blocks, each of which may be a separately nameable operation.

  A short method can still hold two responsibilities.
- **Problem:**
  - Example: a `SaveToFile` that silently appends ".txt" to the name. Low-level path building sits beside file writing, and any other code needing the same path must repeat it.
  - Example: a `SaveToFile` that filters empty strings, joins them and writes them mixes three steps.
- **Detection Signals:**
  - Names containing `And`, `Or`, `If` (`ValidateAndSave`, `LoadOrCreate`, `SaveIfDirty`).
  - Comment headers splitting a body into stages (`// 1. read …`, `// 2. parse …`).
  - Unannounced transformations of inputs (appending extensions, filtering, normalising) that the method name does not mention.
  - Filtering or preparation loops placed before the "real" operation.
- **Recommended Action:** Extract each nameable block into its own method and leave the original as a coordinator. Renaming alone is not enough: changing `SaveToFile` to `SaveNonEmptyToFile` makes the filtering visible but leaves both responsibilities in place. Extract the filter as well (example: `GetNonEmptyOnly`).
- **Exceptions/Trade-offs:** The table describes what each signal *may* indicate. They prompt judgment and are not automatic verdicts.
- **Related Rules:** PCC-067, PCC-070, PCC-071; ch14 Using comments effectively.
- **Review Question:** Does this method contain a block with its own name-worthy purpose, or behavior its name does not announce?

#### PCC-070
**Keep the operations within a method at one consistent level of abstraction**
- **Source Chapter:** 3 — Writing better methods (section: Maintaining consistent levels of abstraction; Note "Practical rule")
- **Principle:** An abstraction shows the idea and hides the machinery. Levels differ in how much detail they hide. A readable method calls operations roughly one level below its own (`BuildHouse` → `BuildWalls` → `PlaceBricks`, `BindBricksWithMortar`, …).
- **Problem:** Mixing levels makes the reader switch between scales, and that switching is what makes code tiring to follow. Two book examples:
  - Flattening wall-level steps directly into `BuildHouse`.
  - Building a path by string concatenation right beside console reporting.
- **Detection Signals:**
  - A method that calls domain-level operations and also does character/string manipulation, raw stream or file handling, or index arithmetic.
  - Sibling calls of unequal granularity (`BuildWalls(); PlaceBricks();`).
  - Inline loops sitting between high-level calls.
- **Recommended Action:** Move lower-level operations into named methods so the parent reads like a list of instructions. Book example: `SaveNonEmptyToFile` = filter, join, write, each a call one level down.
- **Exceptions/Trade-offs:** The book's practical rule is to *prefer* a consistent level and to delegate details *when* exposing them would distract from the primary task. This is a judgment call, not an absolute.
- **Related Rules:** PCC-068, PCC-069, PCC-071.
- **Review Question:** Do all statements in this method sit at about the same conceptual level?

#### PCC-071
**Extract low-level mechanisms into named methods so they are reused, not repeated**
- **Source Chapter:** 3 — Writing better methods (sections: Recognizing methods that do too much; Maintaining consistent levels of abstraction)
- **Principle:** After a low-level mechanism is extracted into its own method, other code that needs it calls that method rather than duplicating the logic.
- **Problem:** Logic buried inside a method must be repeated wherever else it is needed. Book example: file-path building stuck inside `SaveToFile`.
- **Detection Signals:**
  - The same inline expression or loop (path concatenation, empty-string filtering, parsing) appearing in more than one method.
- **Recommended Action:** Extract the method and replace every inline copy with a call to it.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-061, PCC-070, PCC-072.
- **Review Question:** Is this inline low-level logic needed elsewhere, or already duplicated?

#### PCC-072
**Put behavior on the type whose data it uses exclusively**
- **Source Chapter:** 3 — Writing better methods (case study: "Clarifying the signature")
- **Principle:** When a computation needs every value from one type and nothing else, that is a strong signal it belongs on that type. Book example: building a path from directory, name and extension becomes `FileIdentity.AsPath()`.
- **Problem:** Otherwise every consumer re-derives the value, and the type stays a bare data holder with its behavior scattered elsewhere.
- **Detection Signals:**
  - An expression or method that reads two or more members of one parameter object and no other state.
  - The same composition of one object's members repeated across callers.
- **Recommended Action:** Move the computation onto the type as a method. Callers then use `fileIdentity.AsPath()`.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-062, PCC-071; ch13 Balancing coupling, cohesion, and reuse.
- **Review Question:** Does this logic use only one type's data, and so belong on that type?

#### PCC-073
**Order class members top-down: public and high-level first, helpers below**
- **Source Chapter:** 3 — Writing better methods (section: Ordering methods for readability)
- **Principle:** Method order is mainly a reading concern. Where the language allows, readers should meet public behavior and high-level flow before details:
  - Public constructors before ordinary methods, with constructors grouped together.
  - Public methods before private helpers.
  - A caller above the helper it calls, where practical.
  - Overloads kept together, ordered from fewer parameters to more.
- **Problem:** Readers have to hunt for the main flow among implementation details. Top-down order lets them grasp the flow and descend into details only when needed.
- **Detection Signals:**
  - Private helpers declared above the public API.
  - Constructors scattered among methods.
  - Overloads separated from each other.
  - A helper defined far above its only caller.
- **Recommended Action:** Reorder the members. A caller is followed by the helpers it delegates to.
- **Exceptions/Trade-offs:** Language requirements come first. The book calls these readability guidelines, not rigid rules.
- **Related Rules:** PCC-099; ch12 Organizing classes and projects.
- **Review Question:** Can this class be read top-down, meeting the public flow before the helpers?

#### PCC-074
**Make prerequisites explicit; never require a hidden call-order ritual**
- **Source Chapter:** 3 — Writing better methods (section: Keeping methods independent)
- **Principle:** A method must not depend on an undocumented ritual before it can be called. Book example: a PDF reader whose `ReadFrom` fails unless `Init()` was called first. Stable configuration should be required by the constructor, and operation-specific data by method parameters.
- **Problem:** The method's contract hides an important prerequisite. Code that looks correct fails, and callers must remember a magic sequence of unrelated calls or global assignments.
- **Detection Signals:**
  - Public `Init()` / `Initialize()` / `Setup()` / `Configure()` methods that must run before other members work.
  - Guards such as `if (!_initialized) throw new InvalidOperationException(...)`.
  - Static or global properties that must be assigned before a service is usable.
  - Fields that stay null until some other method runs.
  - *(add-in context)* Static holders of `Document` or `UIApplication` that must be set before a helper works.
- **Recommended Action:** Move required setup into constructor parameters, and pass operation-specific data as method parameters.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-065, PCC-075; ch9 Dependency Inversion; ch10 Static methods and dependencies.
- **Review Question:** Can this method be called correctly right after construction, using only its arguments?

#### PCC-075
**Prefer pure functions: results depend only on explicit inputs**
- **Source Chapter:** 3 — Writing better methods (sections: Using pure functions where they fit; Why pure functions are easier to work with; Table 3.2)
- **Principle:** A pure function satisfies two conditions:
  1. Its result depends only on its arguments.
  2. It has no side effects.

  Given the same arguments it returns the same result, and everything it needs is listed in its parameters. Book example: `Max(a, b)`.
- **Problem:** An impure method can reach for its own fields, other classes' statics, globals or the clock, so its real dependencies are invisible in the signature. A method may look like it takes two inputs while reading many more. Book examples:
  - `FindItemWithName` reading an `_items` field.
  - `GetCurrentTime` reading `DateTime.Now`.

  The same arguments can then give different results, and tests need setup, fixtures, mocks, cleanup or environment control (Table 3.2).
- **Detection Signals:**
  - `DateTime.Now`/`UtcNow`, `Environment.*`, or `Random` used inside calculations.
  - Static mutable fields or singletons read inside logic.
  - Computational methods that read instance fields.
  - Methods with few parameters but many references to fields or statics.
- **Recommended Action:** Pass the values the computation needs as parameters, and make parameter-driven helpers `static` where possible (the book's pure `CalculateTotalPrice`).
- **Exceptions/Trade-offs:** Purity applies "where it fits". Not every function can be pure, because programs must talk to users, databases and other systems. Reading object state is the normal job of a stateful object; the example illustrates impurity, it does not condemn it.
- **Related Rules:** PCC-065, PCC-074, PCC-076, PCC-077; ch10 Static methods and dependencies; ch15 Writing testable code and clean tests.
- **Review Question:** Could this calculation be expressed as a function of its parameters alone?

#### PCC-076
**Avoid hidden side effects, especially mutating caller-supplied arguments**
- **Source Chapter:** 3 — Writing better methods (sections: Using pure functions where they fit; Why pure functions are easier to work with)
- **Principle:** A side effect is any change a function makes beyond its return value: writing a field, modifying a collection it was given, or touching a file, database or UI state. Side effects make changes hard to trace.
- **Problem:** A list passed to an innocent-looking method can come back modified (book example: `RemoveFirstItem` calling `RemoveAt(0)` on its argument). When any call may change state behind the scenes, no assumption about program state is safe.
- **Detection Signals:**
  - `Add`/`Remove`/`RemoveAt`/`Clear`/`Sort` called on a collection *parameter*.
  - Property setters invoked on parameter objects.
  - `void` methods taking a collection.
  - Query-named methods (`Get*`, `Find*`, `Is*`, `Calculate*`) that write fields or files.
- **Recommended Action:** Return a new result instead of mutating the input (book example: `GetNonEmptyOnly` builds and returns a new list). Where effects are unavoidable, keep them at the boundary (PCC-077).
- **Exceptions/Trade-offs:** Side effects are unavoidable where the program meets the outside world (PCC-077).
- **Related Rules:** PCC-075, PCC-077.
- **Review Question:** Does this method change anything other than its return value that the caller would not expect?

#### PCC-077
**Keep impurity at the boundaries; build the core from pure functions**
- **Source Chapter:** 3 — Writing better methods (section: Why pure functions are easier to work with)
- **Principle:** Aim for two zones: effectful code confined to the edges where the program touches users, files, databases and other systems, and a core made of pure functions.
- **Problem:** When impurity is spread everywhere, reasoning and testing require knowledge of external state at every step.
- **Detection Signals:**
  - Domain calculations interleaved with Console, File, database or UI calls.
  - Logic that cannot be exercised by a test without performing I/O.
  - *(add-in context)* Geometry or rebar arithmetic written inside methods that also open transactions or call the Revit API.
- **Recommended Action:** Move computation into pure functions, and have thin I/O-facing methods call them.
- **Exceptions/Trade-offs:** Apply where it fits. Don't force purity onto code whose job *is* the interaction.
- **Related Rules:** PCC-075, PCC-076; ch15 Writing testable code and clean tests.
- **Review Question:** Is the decision and calculation logic separated from the code that talks to the outside world?

#### PCC-078
**Validate prerequisites early, before the method does other work**
- **Source Chapter:** 3 — Writing better methods (case study: "Validating early and extracting constants")
- **Principle:** Check prerequisites (the file exists, the extension is supported) at the start. Early validation simplifies the rest of the flow.
- **Problem:** The original method discovered an unsupported extension only after other work, in a final `else` deep inside the parsing branches.
- **Detection Signals:**
  - `else { report-error; return; }` at the end of an `if / else if` chain.
  - Precondition checks placed after I/O or expensive work.
  - Nesting depth caused by validations wrapped around the main logic.
- **Recommended Action:** Move validations to the top as early returns or refusals, so the remaining body handles only the valid case.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-069, PCC-079, PCC-083.
- **Review Question:** Are all preconditions checked before the method starts its real work?

#### PCC-079
**Define a set of supported values once, as named constants**
- **Source Chapter:** 3 — Writing better methods (case study: "Identifying the issues"; "Validating early and extracting constants")
- **Principle:** Hard-coded literals embedded in branches (`"txt"`, `"json"`) mean the set of supported values is not defined in one place. Extract them into constants.
- **Problem:** The definition of what is supported is scattered across conditions.
- **Detection Signals:**
  - The same string or number literal compared in several places (`== "txt"`).
  - Magic literals in `if` or `switch` conditions.
- **Recommended Action:** Declare named constants (book example: `private const string Txt = "txt";`) and reference them everywhere.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-078; ch2 Meaningful names.
- **Review Question:** Is the set of allowed values defined in exactly one place?

#### PCC-080
**Name variables and parameters for what they actually hold**
- **Source Chapter:** 3 — Writing better methods (case study: "Identifying the issues"; "Validating early and extracting constants")
- **Principle:** A local holding a path should be `filePath`, not `file`. A parameter for an extension should not be `ex`, which conventionally means an exception. Precise names remove a source of mental translation.
- **Problem:** Misleading or convention-clashing names force the reader to translate while reading.
- **Detection Signals:**
  - Abbreviations colliding with common conventions (`ex`, `e` used for something other than an exception).
  - A variable named after a type it does not hold (`file` holding a string path).
  - Terse names such as `dir` or `name` whose role is unclear.
- **Recommended Action:** Rename to the content's real meaning.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-057; ch2 Meaningful names.
- **Review Question:** Does each name describe the value it holds, without clashing with common conventions?

#### PCC-081
**Prefer direct collection operations over hand-written loops**
- **Source Chapter:** 3 — Writing better methods (case study: "Extracting file-reading responsibilities"; "Simplifying the search and printing the result"; Case-study lesson)
- **Principle:** Book examples: a manual `foreach` search is replaced by `Contains`, and a split-and-parse loop by a `Split`/`Select`/`ToList` chain. The case-study lesson lists direct collection operations among the improvements that reinforce one another.
- **Problem:** Manual loops add low-level noise to a method that should read at a higher level.
- **Detection Signals:**
  - A `foreach` with `if (item == target) { …; return; }`.
  - A `foreach` whose only job is adding transformed items to a new list.
- **Recommended Action:** Replace with the equivalent BCL or LINQ operation.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-070, PCC-097.
- **Review Question:** Is there a manual loop that a standard collection operation would express more directly?

#### PCC-082
**Use lambdas for behavior needed at a single call site; delegates to pass behavior**
- **Source Chapter:** 3 — Writing better methods (section: Delegates and lambda expressions)
- **Principle:** Delegates are C#'s type-safe way to store or pass callable behavior: `Func` for methods that return a value, `Action` for `void`. Lambdas are concise anonymous functions, especially useful when the behavior is needed at only one call site (book example: a predicate passed to `Where`).
- **Problem:** (not stated in book)
- **Detection Signals:** (not stated in book)
- **Recommended Action:** Pass behavior as `Func`/`Action`, and write single-use predicates inline as lambdas.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-081.
- **Review Question:** Is one-off behavior expressed inline as a lambda, and reusable behavior as a named method or delegate?

#### PCC-083
**Refactor a large method as a sequence of focused, behavior-preserving steps**
- **Source Chapter:** 3 — Writing better methods (section: Refactoring case study: Improving method design; Note "Case-study lesson")
- **Principle:** Work in three stages: understand the code, then list the issues, then apply focused changes one at a time. The changes are: signature (names, parameter order, grouping, behavior moved onto the new type), early validation and constants, extraction of readers, a simpler search, and extraction of output. The refactoring changes how clearly the behavior is communicated, not the behavior itself.
- **Problem:** Code that can only be understood by reading every line, and ideally running it. Good code should not demand that.
- **Detection Signals:**
  - A method understandable only by full read-through.
  - Several Chapter 3 smells co-occurring in one method: noun name, vague class, unclear and misordered parameters, one concept spread over several parameters, hard-coded literals, mixed levels.
- **Recommended Action:** Follow the case-study sequence:
  1. Rename the class and method to reveal intent.
  2. Reorder parameters to their natural order.
  3. Group a real concept into a type.
  4. Move computation onto that type.
  5. Validate early and extract constants.
  6. Extract lower-level readers.
  7. Replace manual search with a collection operation.
  8. Extract result printing.

  The top-level method then reads as: validate, read, decide, report.
- **Exceptions/Trade-offs:** Intended behavior must not change. The book stresses the improvements are cumulative and reinforce one another, not isolated rules.
- **Related Rules:** PCC-057, PCC-062, PCC-066, PCC-070, PCC-072, PCC-078–PCC-081.
- **Review Question:** Did this refactoring change only how clearly the behavior is expressed, not the behavior?

### Code smells & anti-patterns catalogue

| Name | Signal | Rule IDs |
|---|---|---|
| Noun-named method / catch-all class | `IdFinder()`, `FileManager`, bool method not phrased as a question | PCC-057 |
| Ambiguous call site | generic verb + literal (`Save("x")`), append vs overwrite unclear | PCC-058 |
| Opaque signature | primitives standing for concepts; result knowable only from the body | PCC-059 |
| Long parameter list | many primitives / adjacent same-typed strings | PCC-060 |
| Multi-job method inflating parameters | parameters used only to compute an intermediate value | PCC-061 |
| Loose related parameters | shared prefixes, coordinate pairs, same cluster repeated across methods | PCC-062 |
| Meaningless parameter wrapper | a type created only to cut the count | PCC-062, PCC-064 |
| Boolean flag argument | `Foo(a, b, true)`; branches doing different jobs | PCC-063 |
| Parameter hidden in a field | field read by one method; property set right before the call | PCC-065 |
| Illogical parameter order | order contradicts hierarchy, reading order or method name | PCC-066 |
| Long method | > ~20 lines or > ~120 chars (warning only) | PCC-067 |
| Silent extra behavior | method appends, filters or normalises without saying so | PCC-069 |
| AND / OR / IF in the only accurate name | `ValidateAndSave`, `LoadOrCreate` | PCC-069 |
| Commented blocks inside a method | `// step N` headers | PCC-069 |
| Mixed abstraction levels | domain calls next to string/stream/index manipulation | PCC-070 |
| Duplicated low-level mechanism | the same path/filter/parse code in several methods | PCC-071, PCC-072 |
| Behavior away from its data | logic using only one type's members lives elsewhere | PCC-072 |
| Members out of reading order | private helpers above public API, scattered constructors or overloads | PCC-073 |
| Hidden setup ritual | required `Init()`, `_initialized` guard, global assignment before use | PCC-074 |
| Hidden inputs | `DateTime.Now`, statics, singletons, fields read in calculations | PCC-075 |
| Argument mutation / hidden side effect | `RemoveAt`/`Clear` on a parameter; query-named method writing state | PCC-076 |
| Impure core | calculations interleaved with I/O | PCC-077 |
| Late validation | `else { error; return; }` after work is done | PCC-078 |
| Hard-coded supported values | repeated `"txt"` literal comparisons | PCC-079 |
| Misleading variable name | `file` holding a path, `ex` for extension | PCC-080 |
| Manual search / transform loop | `foreach … if (== target)` | PCC-081 |

### Refactoring techniques named in the chapter
- Rename method or class to a verb phrase or precise purpose (`IdFinder` → `CheckIfIdExistsInFile`; `FileManager` → `IdExistenceChecker`).
- Rename a method so the call site disambiguates the argument's role (`AppendText`, `ReplaceContentWith`, `WriteTo`).
- Split a method around responsibilities so each needs fewer parameters (`BuildFileName` extracted from `SaveGame`).
- Group related parameters into a struct or record (`Point`, `DatabaseIdentity`, `UserCredentials`, `FileIdentity`).
- Replace a Boolean flag with a unit-named method plus a separate converter (`CalculateDistanceInKilometers` + `UnitConverter.KilometersToMiles`).
- Keep an operation's input as a parameter rather than a field (the static `CalculateTotalPrice`).
- Reorder parameters by hierarchy, reading order or method-name order.
- Extract method for filtering, parsing and printing (`GetNonEmptyOnly`, `ReadIdsFromFile`/`ReadIdsFromText`/`ReadIdsFromJson`, `PrintResult`).
- Move a computation onto the type whose data it uses (`FileIdentity.AsPath()`).
- Validate early with early returns; extract literals into constants.
- Replace a manual loop with a collection operation (`Contains`, `Split`/`Select`/`ToList`).
- Make prerequisites constructor or method parameters instead of an `Init()` ritual.
- Reorder class members top-down (constructors, public, then helpers; overloads together).
- Push side effects to the boundary and keep a pure core.

### Things the author says NOT to do mechanically
- Do not chase an arbitrary method length or parameter count. 20 lines / 120 characters are warning thresholds: a cohesive 25-line method can beat five artificial helpers, and a 6-line method can still mix responsibilities.
- Do not treat a long parameter list as automatically wrong. It is a prompt for a design question.
- Do not cut parameters at any cost. `DateTime(…)`, `AreEqualWithinTolerance(a, b, tolerance)` and `CalculateTotalPrice(quantity, unitPrice, tax)` legitimately take several values.
- Do not create a wrapper type just to lower the count; it hides the problem.
- Do not move a per-operation input into a field to shorten a signature.
- Do not treat every Boolean parameter as a flag. Plain data such as `isAdmin` feeding one decision is fine.
- Do not assume renaming fixes mixed responsibilities. `SaveNonEmptyToFile` still needed the filter extracted.
- Do not count an orchestrating method as "doing several things". Coordination is one task.
- Method ordering rules are readability guidelines, not rigid rules; language requirements come first.
- Do not force purity everywhere. Not every function can be pure, so keep impurity at the boundaries.

---

## Chapter 4 — Formatting code

### Chapter summary
- Correct code can still be hard to work with. Inconsistent indentation, overlong expressions, unpredictable braces and packed statements make readers rebuild the structure before they can think about behavior.
- Formatting (indentation, spacing, line breaks, braces, visual separation) does not change behavior. Its purpose is to make the structure already present in the program easy to recover. Readers rely on visual patterns, as with typography in prose.
- Formatting rules come from three sources with different strictness: language syntax, language or community convention, and team standard. No single style is right everywhere, so **consistency beats preference**, and team-specific decisions that matter should be documented.
- Formatting is repetitive and objective once chosen, so automate it (IDE formatter, inspections, analyzers, CI checks) with committed configuration. Review time then goes to design, correctness and naming.
- Concrete practices:
  - Consistent indentation per scope, and no mixture of tabs and spaces.
  - Braces even for single statements.
  - Blocks that fit a screen (~120 × 20).
  - Line breaks at meaningful points, all-or-nothing for parameter lists and chains.
  - Single blank lines between logical units.
- Formatting doubles as a diagnostic: when a block is still too large after formatting, extract it into a named method. The Hangman case study ends longer but readable.

### Rules

#### PCC-084
**Make the visual layout reveal the program's logical structure**
- **Source Chapter:** 4 — Formatting code (section: Why formatting matters)
- **Principle:** Formatting governs how quickly someone can understand and safely modify code, even though it does not change behavior. Its central purpose is to reduce the effort of recovering the structure that is already there.
- **Problem:** When code is visually compressed or inconsistent, the reader must reconstruct nesting, branches and boundaries first. Book example: `AreAllEven` with the loop, condition and return squeezed together, versus the expanded version where the loop, condition and return paths are obvious at once.
- **Detection Signals:**
  - Several statements on one line.
  - A control statement and its body on the same line (`if (…) return false;` inside a one-line loop).
  - Nesting that is not visible without reading the expressions.
- **Recommended Action:** Lay out each loop, condition and exit on its own lines with braces and indentation, accepting the extra vertical space.
- **Exceptions/Trade-offs:** It costs vertical space. The book explicitly accepts longer code: the refactored Hangman is longer, and that length is what makes it readable.
- **Related Rules:** PCC-090, PCC-092, PCC-101.
- **Review Question:** Can you see the nesting, branches and exits at a glance, without parsing expressions?

#### PCC-085
**Know where a formatting rule comes from, to decide how strictly to follow it**
- **Source Chapter:** 4 — Formatting code (section: Choosing and following a formatting style; Table 4.1)
- **Principle:** Formatting rules come from three sources (Table 4.1):
  - **Language syntax:** changing it alters or invalidates the program (Python indentation).
  - **Language or community convention:** the compiler allows alternatives but the ecosystem expects a familiar form (C# casing and brace conventions).
  - **Team or project standard:** one choice among acceptable options (brace placement, tab width, line-breaking rules).
- **Problem:** Without the distinction, teams either treat preferences as law or treat conventions as optional. In C#, braces, not indentation, define blocks, so bad indentation misleads rather than changes semantics. In Python the same mistake changes or breaks the program.
- **Detection Signals:**
  - Deviations from established C# casing or brace conventions.
  - Inconsistent choices where a team standard exists.
- **Recommended Action:** Treat syntax as mandatory and C# community conventions as the default. Let the team pick one option where several are acceptable.
- **Exceptions/Trade-offs:** There is no single correct style for every language, project or team.
- **Related Rules:** PCC-086, PCC-087, PCC-090.
- **Review Question:** Does this code follow C# community conventions and the team's agreed choices?

#### PCC-086
**Apply the same formatting choice everywhere; consistency beats personal preference**
- **Source Chapter:** 4 — Formatting code (section: Choosing and following a formatting style)
- **Principle:** A codebase is easier to read when the same choice is applied everywhere. Once readers learn the project pattern, the visual structure becomes predictable and attention stays on behavior.
- **Problem:** Mixed styles make every file a new layout to decode.
- **Detection Signals:**
  - The same construct formatted differently across files or authors (brace placement, wrapping style, blank line before `return`).
  - A change that introduces a style different from the surrounding file.
- **Recommended Action:** Follow the project pattern even where you would personally choose otherwise.
- **Exceptions/Trade-offs:** For preference-level choices (for example, a blank line before a final `return`) either option is fine as long as it is applied consistently.
- **Related Rules:** PCC-085, PCC-088, PCC-099.
- **Review Question:** Does this change match the formatting already used in the codebase?

#### PCC-087
**Document team conventions beyond the mainstream style, but only the ones that matter**
- **Source Chapter:** 4 — Formatting code (section: Choosing and following a formatting style)
- **Principle:** When a team adopts conventions beyond the language's mainstream style, write them down. A written standard ends repeated debates and gives newcomers a reference.
- **Problem:** Undocumented local conventions get re-argued and are unknown to new members.
- **Detection Signals:**
  - The same style point raised repeatedly in reviews.
  - No written or encoded record of local conventions.
- **Recommended Action:** Record the decisions that materially affect consistency, and encode them in tooling (PCC-088).
- **Exceptions/Trade-offs:** The goal is not an exhaustive rulebook covering every possible line.
- **Related Rules:** PCC-086, PCC-088.
- **Review Question:** Is every non-mainstream convention used here documented for the team?

#### PCC-088
**Automate formatting and commit the configuration to the repository**
- **Source Chapter:** 4 — Formatting code (section: Using automated formatting tools; Table 4.2)
- **Principle:** Once a style is chosen, formatting rules are repetitive and objective, which makes them ideal for automation. Table 4.2 lists the tools:
  - IDE formatter (for example, Visual Studio *Format Document*).
  - IDE inspections and quick fixes.
  - Linters and analyzers (ESLint, SonarQube for IDE).
  - CI/CD quality checks (SonarQube Server).

  CI checks apply the rules on every push, independent of each developer's editor setup. The default formatter is only a starting point: configure it to project conventions and commit the configuration files.
- **Problem:** Formatting that depends on individual editor settings drifts.
- **Detection Signals:**
  - No shared, committed formatter or analyzer configuration in the repository.
  - Diffs containing whitespace-only churn.
  - No automated style check in CI.
- **Recommended Action:** Configure IDE formatting to project rules, commit the configuration, and add analyzer or CI enforcement.
- **Exceptions/Trade-offs:** Automatic reformatting alone is not enough; some changes need judgment (PCC-102, PCC-103).
- **Related Rules:** PCC-087, PCC-089, PCC-091, PCC-102.
- **Review Question:** Is formatting enforced by shared, committed tooling rather than by each developer's editor?

#### PCC-089
**Keep routine formatting out of code review**
- **Source Chapter:** 4 — Formatting code (section: Using automated formatting tools)
- **Principle:** Automated tools should take routine formatting work out of review, so reviewers can concentrate on design, correctness, naming and maintainability.
- **Problem:** Reviewers repeatedly pointing out spacing or brace placement waste review attention.
- **Detection Signals:**
  - Review threads dominated by spacing, brace or indentation comments.
- **Recommended Action:** Fix the tooling gap instead of commenting on each instance; keep review comments for design-level issues.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-088.
- **Review Question:** Is this review spending time on things a formatter should have caught?

#### PCC-090
**Indent every nested scope consistently, one level per nesting**
- **Source Chapter:** 4 — Formatting code (section: Applying indentation consistently)
- **Principle:** Indentation shows what belongs to what. Every construct that introduces a scope (loops, conditions, try/catch, methods, classes) is visually offset from the level above it, so the structure can be taken in before any expression is read.
- **Problem:** An unindented block may compile, but it hides its nesting. The original Hangman had misaligned closing braces.
- **Detection Signals:**
  - Statements at the same column as their enclosing `for`/`if`.
  - Misaligned closing braces.
  - `try`/`catch` bodies not offset from their keyword.
- **Recommended Action:** Reindent, preferably with the formatter.
- **Exceptions/Trade-offs:** In C# braces, not indentation, define blocks, so wrong indentation misleads rather than changes meaning (contrast Python).
- **Related Rules:** PCC-084, PCC-085, PCC-091, PCC-092.
- **Review Question:** Does each nesting level sit visibly offset from its parent?

#### PCC-091
**Use one whitespace convention (tabs or spaces) across the repository**
- **Source Chapter:** 4 — Formatting code (section: Applying indentation consistently)
- **Principle:** A project may use tabs, spaces, or tooling that converts between them. What matters is that the repository does not contain a mixture.
- **Problem:** Mixed whitespace makes alignment shift between editors.
- **Detection Signals:**
  - Files mixing tab-indented and space-indented lines.
  - Alignment that looks different in different editors.
  - Whitespace-only diffs.
- **Recommended Action:** Agree on one choice and configure the development environment to apply it automatically.
- **Exceptions/Trade-offs:** Which character is used is a team choice; only the mixture is wrong.
- **Related Rules:** PCC-088, PCC-090.
- **Review Question:** Is the indentation whitespace uniform and matching the repository's agreed setting?

#### PCC-092
**Always brace block bodies, even single statements**
- **Source Chapter:** 4 — Formatting code (section: Structuring code blocks safely; case study "Make scopes explicit")
- **Principle:** In brace-delimited languages, keep braces even when a block has only one statement. Braces make scope unmistakable, and nobody has to remember to add them when the block grows.
- **Problem:** Omitting braces saves two lines but invites a bug. Book example: a second indented statement added under a brace-less `for` looks like part of the loop but is not.
- **Detection Signals:**
  - `if (…)`, `else`, `for`, `foreach` or `while` followed by a statement without `{`.
  - `if (…) stmt; else stmt;` on one line.
- **Recommended Action:** Add braces consistently to every control-flow body.
- **Exceptions/Trade-offs:** (not stated in book). The book presents this as a useful safety practice.
- **Related Rules:** PCC-084, PCC-090, PCC-101.
- **Review Question:** Does every control-flow body have explicit braces?

#### PCC-093
**Extract a block whose opening and closing braces don't fit on one screen**
- **Source Chapter:** 4 — Formatting code (section: Structuring code blocks safely)
- **Principle:** If a loop or conditional is so large that its braces cannot be seen together on a reasonable screen, that usually signals an opportunity to extract part of it into a well-named method. The common guideline is 120 characters wide by 20 lines high, roughly one screen without scrolling.
- **Problem:** The reader has to scroll to recover the block's structure.
- **Detection Signals:**
  - `if`, loop or `try` bodies longer than about 20 lines.
- **Recommended Action:** Extract the block, or part of it, into a method whose name explains the step.
- **Exceptions/Trade-offs:** The 120 × 20 figure is a guideline, about one screen.
- **Related Rules:** PCC-067, PCC-094, PCC-103.
- **Review Question:** Can you see this block's start and end without scrolling?

#### PCC-094
**Respect a line-length limit, breaking at meaningful points before you reach it**
- **Source Chapter:** 4 — Formatting code (section: Breaking long lines clearly; Table 4.3)
- **Principle:** Long lines force horizontal scrolling and make related expressions hard to compare. Each project can set its own limit (120 is the common guideline). More important than the number is breaking at meaningful points, consistently across the codebase.
- **Problem:** Overlong lines, or lines broken at arbitrary positions, hide structure.
- **Detection Signals:**
  - Lines beyond the project limit.
  - Breaks at arbitrary character positions, such as mid-phrase inside a string literal (seen in the original Hangman).
- **Recommended Action:** Break at logical components (PCC-095–PCC-098). Do not wait until a line exceeds the limit if an earlier break reads better.
- **Exceptions/Trade-offs:** The exact limit is a project choice. Readability is the objective; the character count is only a guardrail.
- **Related Rules:** PCC-067, PCC-095, PCC-096, PCC-097, PCC-098.
- **Review Question:** Are lines within the limit and broken where the meaning divides?

#### PCC-095
**Put comparable Boolean conditions on separate, aligned lines**
- **Source Chapter:** 4 — Formatting code (section: Breaking long lines clearly; Table 4.3)
- **Principle:** When a condition combines comparable sub-conditions, place each on its own line, broken the same way, so the differences between them stand out. Book example: a tic-tac-toe row check of three cells.
- **Problem:** On one long line, near-identical comparisons are hard to compare.
- **Detection Signals:**
  - Long `&&`/`||` chains of similar comparisons on a single line.
- **Recommended Action:** One sub-condition per line, consistently broken and aligned. The book's example ends each line with `&&`.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-094.
- **Review Question:** Can the differences between these conditions be seen by scanning vertically?

#### PCC-096
**Break parameter lists all-or-nothing**
- **Source Chapter:** 4 — Formatting code (section: Breaking long lines clearly; Table 4.3)
- **Principle:** When a signature gets crowded, put each parameter on its own line with one consistent indent. Either all parameters get their own line or none do.
- **Problem:** Partial wrapping (three on one line, two on the next) is harder to scan than any consistent choice.
- **Detection Signals:**
  - Signatures or calls where some lines hold several parameters and others hold one.
- **Recommended Action:** Reformat to one parameter per line, or all on one line.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-060, PCC-094.
- **Review Question:** Are the parameters either all on one line or each on its own line?

#### PCC-097
**Break long method chains one call per line, with the dot leading**
- **Source Chapter:** 4 — Formatting code (section: Breaking long lines clearly; Table 4.3)
- **Principle:** A short chain of two calls reads fine on one line. Once a chain grows, break all of it: each transformation on its own line, starting with the dot. This makes the pipeline visible step by step.
- **Problem:** Long or partially wrapped chains hide the sequence of transformations.
- **Detection Signals:**
  - LINQ or fluent chains of three or more calls on one line.
  - Chains wrapped only partially.
- **Recommended Action:** Put each chained call on its own line with a leading `.`.
- **Exceptions/Trade-offs:** A two-call chain may stay on one line.
- **Related Rules:** PCC-081, PCC-094.
- **Review Question:** Is each step of a longer chain on its own line?

#### PCC-098
**Break long interpolated or concatenated expressions at logical components**
- **Source Chapter:** 4 — Formatting code (section: Breaking long lines clearly; Table 4.3; case study `PrintGameResult`)
- **Principle:** Split long string-building expressions where their meaning divides, not at arbitrary character positions, so the visual split follows the meaning. In the Hangman refactoring the result message and the "press any key" prompt also became separate statements.
- **Problem:** Strings wrapped wherever the line ran out read as broken phrases.
- **Detection Signals:**
  - String literals split mid-phrase.
  - Concatenations wrapped at the edge regardless of meaning.
- **Recommended Action:** Break between components. Where an output mixes separate messages, emit them separately.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-094.
- **Review Question:** Does each line break fall between meaningful parts of the expression?

#### PCC-099
**Use single blank lines to separate logical units, not to fragment one construct**
- **Source Chapter:** 4 — Formatting code (section: Using vertical space to organize code; case study "Make scopes explicit")
- **Principle:** Blank lines act like paragraph breaks. Use them to:
  - Separate methods.
  - Separate fields from the constructor.
  - Separate meaningful stages inside a larger method (declarations, iteration, output in the `CountEvenAndOdd` example; display, read, process in the Hangman loop).

  Inside one construct, such as an `if` and its `else`, keep the lines visually connected.
- **Problem:** With no separation, related statements can't be distinguished from unrelated ones. Unnecessary blank lines inside one construct fragment it.
- **Detection Signals:**
  - Members with no blank line between them.
  - A constructor glued to the field declarations above it.
  - Long methods with no visual stages.
  - Blank lines between `}` and `else`, or inside small constructs.
- **Recommended Action:** Add a single blank line at real boundaries and remove those that split one construct.
- **Exceptions/Trade-offs:** Exact placement is partly a style decision (for example, a blank line before a final `return` in larger methods but not in tiny ones). Consistency matters more than which option is chosen.
- **Related Rules:** PCC-069, PCC-073, PCC-086, PCC-100.
- **Review Question:** Do blank lines mark real boundaries between ideas, and only those?

#### PCC-100
**Never use more than one consecutive blank line**
- **Source Chapter:** 4 — Formatting code (section: Using vertical space to organize code)
- **Principle:** One empty line is enough to separate concepts.
- **Problem:** Multiple consecutive blank lines lengthen files and add scrolling without adding meaning.
- **Detection Signals:**
  - Two or more consecutive empty lines (regex such as `\n[ \t]*\n[ \t]*\n`).
- **Recommended Action:** Collapse them to a single blank line.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-099.
- **Review Question:** Are there runs of two or more blank lines?

#### PCC-101
**Don't compact declarations or block bodies onto a single line**
- **Source Chapter:** 4 — Formatting code (case study: "Identifying the issues"; "Apply the formatter before editing manually")
- **Principle:** Compacted members hide structure. Book examples: a constructor whose braced body sits on the declaration line, a helper whose `return` shares a line with its braces, `} return true;`, and `if (…) Console.Write(…); else …` on one line. Put the braces and body on separate lines.
- **Problem:** Structure disappears into dense lines. The book notes the formatter will not always fix this, so it needs a manual pass.
- **Detection Signals:**
  - A brace-bodied member with `{ … }` on its declaration line.
  - Statements following a closing `}` on the same line.
  - Conditional statements with both branches on one line.
- **Recommended Action:** Expand to one statement per line with braces on their own lines, and separate the constructor from the fields with a blank line.
- **Exceptions/Trade-offs:** (not stated in book) The book's own Chapter 3 examples use expression-bodied members (`=>`) for one-line methods (for example `AsPath() => …`), so this rule targets brace bodies squeezed onto one line, not `=>` members.
- **Related Rules:** PCC-084, PCC-092, PCC-099, PCC-102.
- **Review Question:** Is any brace-bodied member or block squeezed onto its declaration line, or written after a closing brace?

#### PCC-102
**Run the automated formatter first, then make the edits that need judgment**
- **Source Chapter:** 4 — Formatting code (case study: "Apply the formatter before editing manually"; Table 4.4)
- **Principle:** The first step is to let the IDE normalise indentation, spacing and brace alignment. That quickly removes mechanical inconsistencies and gives a stable baseline for the changes that need judgment.
- **Problem:** Manual reformatting of mechanical issues is slow and inconsistent, and automatic reformatting alone leaves judgment calls undone (for example, compacted constructors).
- **Detection Signals:** (not stated in book beyond the workflow) For example, manual whitespace edits in code that has never been run through the formatter.
- **Recommended Action:** Run Format Document, then fix what the formatter cannot (PCC-101), add braces (PCC-092), add blank lines (PCC-099) and extract oversized blocks (PCC-103).
- **Exceptions/Trade-offs:** The formatter is necessary but not sufficient.
- **Related Rules:** PCC-088, PCC-101, PCC-103.
- **Review Question:** Was the mechanical normalisation done by the tool before the manual formatting work?

#### PCC-103
**Use formatting as a diagnostic: extract blocks that remain too large after formatting**
- **Source Chapter:** 4 — Formatting code (case study: "Extract code when formatting reveals an oversized block"; Table 4.4)
- **Principle:** Once the basic formatting is fixed, a logical block that is still too big to scan comfortably should be extracted into a method whose name explains the step. Book example: Hangman's `while` body became `AttemptToGuessLetter`, supported by `PrintGameStatus`, `GuessLetter` and `PrintGameResult`. Most IDEs offer extract-method.
- **Problem:** An oversized loop body obscures the method's high-level flow. Several visually distinct sections in one method each lack a name.
- **Detection Signals:**
  - A loop or branch body that dominates its method after formatting.
  - A method with several visually separated sections (each one an extraction candidate).
- **Recommended Action:** Extract each section into a well-named method, so the parent shows the high-level flow (`Play` = loop over `AttemptToGuessLetter`, then `PrintGameResult`).
- **Exceptions/Trade-offs:** Behavior must be preserved. The resulting file is longer, which the book accepts because the compressed original had lost its structure.
- **Related Rules:** PCC-069, PCC-070, PCC-093, PCC-102.
- **Review Question:** Once formatted, does this method's high-level flow read at a glance?

### Code smells & anti-patterns catalogue

| Name | Signal | Rule IDs |
|---|---|---|
| Compressed control flow | loop, condition and return on one line | PCC-084, PCC-101 |
| Inconsistent style | same construct formatted differently across the codebase | PCC-086 |
| Undocumented local convention | recurring review debate on the same style point | PCC-087 |
| Editor-dependent formatting | no committed formatter/analyzer config; whitespace churn in diffs | PCC-088, PCC-091 |
| Formatting nitpicks in review | review threads about spacing and braces | PCC-089 |
| Inconsistent or missing indentation | statements flush with their enclosing `for`/`if`; misaligned `}` | PCC-090 |
| Mixed tabs and spaces | alignment changes between editors | PCC-091 |
| Brace-less single-statement body | `if (…) stmt;` / `for (…) stmt;` | PCC-092 |
| Off-screen block | braces of one block more than ~20 lines apart | PCC-093, PCC-103 |
| Overlong line / arbitrary break | > limit; strings split mid-phrase | PCC-094, PCC-098 |
| One-line condition soup | long `&&`/`||` chain of similar comparisons | PCC-095 |
| Partially wrapped parameter list | some lines with several parameters, others with one | PCC-096 |
| One-line long chain / half-wrapped chain | 3+ fluent calls on one line | PCC-097 |
| No vertical separation | members or stages glued together | PCC-099 |
| Fragmented construct | blank line between `}` and `else` | PCC-099 |
| Multiple blank lines | 2+ consecutive empty lines | PCC-100 |
| Compacted members | `{ body }` on the declaration line; `} return x;` | PCC-101 |
| Oversized loop body | loop hides the method's flow after formatting | PCC-103 |

### Refactoring techniques named in the chapter
- IDE *Format Document* (or format selection) to normalise indentation, spacing and brace alignment.
- Configure formatter settings to project conventions and commit the configuration files; enforce with inspections, analyzers and CI quality checks.
- Add braces to every single-statement block.
- Reindent nested scopes and unify tabs versus spaces.
- Break lines at logical points: aligned Boolean sub-conditions, one parameter per line (all-or-nothing), one chained call per line with a leading dot, split strings at their components.
- Insert single blank lines between members and between stages of a method; remove extra or fragmenting blank lines.
- Expand compacted constructors and helpers onto multiple lines.
- IDE *Extract Method* for oversized blocks revealed by formatting (`AttemptToGuessLetter`, `PrintGameStatus`, `GuessLetter`, `PrintGameResult`).

### Things the author says NOT to do mechanically
- Do not treat any single formatting style as universally correct. Syntax rules are fixed, but convention and team choices vary.
- Do not insist on personal preference over the project's consistent choice.
- Do not write an exhaustive rulebook; document only decisions that materially affect consistency.
- Do not rely on the default formatter as-is; it is a starting point to configure.
- Do not assume the automatic formatter is enough; some fixes need judgment.
- The 120-character / 20-line figures are guidelines (about one screen) and each project sets its own limit. Do not wait for a line to hit the limit if an earlier break reads better.
- Do not treat blank-line placement such as before a final `return` as a universal rule; it is a team style choice.
- Do not judge formatted code as worse because it is longer; the extra vertical space is what restores readability.

---

## Chapter 5 — Applying the Single Responsibility Principle

### Chapter summary
- Classes grow by accumulation. A new requirement lands in the nearest roughly related class, nobody decides to add a second responsibility, and eventually changing one part breaks another.
- SOLID (SRP, OCP, LSP, ISP, DIP) is a vocabulary for recognising design problems and reasoning about alternatives, not a set of mechanical rules that guarantee good design.
- SRP: a class has one coherent responsibility, i.e. the coherent purpose it serves. Equivalently, it has no more than one reason to change. "Responsibility" must be interpreted carefully.
- Combining unrelated responsibilities (data access plus formatting) has concrete costs (Table 5.2): forced dependencies, reduced flexibility, combinatorial growth of classes (the jacket-and-backpack analogy), harder maintenance, hidden interaction risk, less focused tests, poor names and more merge conflicts. These costs come from mixed reasons to change, **not** from line count.
- One responsibility ≠ one method. `List<T>` and a repository have many methods serving one purpose, and splitting them per operation adds coupling and weakens invariants. Orchestrators that delegate detailed work still have one responsibility.
- Practical evaluation tools:
  - The one-reason-to-change test.
  - Noticing who requests changes.
  - The Table 5.3 checklist: one-sentence description, whether the split improves design, coupling of the new classes, multiple reasons to change, orchestration, vague names.
  - A refactoring experiment that is reverted if the split turns out artificial.

### Rules

#### PCC-104
**Use SOLID as a vocabulary for reasoning, not as a mechanical checklist**
- **Source Chapter:** 5 — Applying the Single Responsibility Principle (section: Introducing the SOLID principles; Table 5.1)
- **Principle:** SOLID is five object-oriented principles that help keep software understandable, maintainable, extensible and testable (Table 5.1):
  - **S:** one coherent responsibility.
  - **O:** extend without repeatedly modifying.
  - **L:** subtypes safely substitute for their base types.
  - **I:** clients do not depend on operations they don't need.
  - **D:** depend on abstractions, not concrete details.

  They give a vocabulary for spotting problems and reasoning about alternatives; they do not guarantee good design.
- **Problem:** Applying them mechanically produces wrong conclusions, such as counting methods (PCC-109) or splitting orchestrators (PCC-110).
- **Detection Signals:**
  - Review comments that cite a principle by name with no concrete consequence attached (no reason to change, coupling or dependency identified).
- **Recommended Action:** Argue in terms of design consequences (reasons to change, forced dependencies, coupling) and use the principle names as shorthand.
- **Exceptions/Trade-offs:** The principles are explicitly not mechanical rules.
- **Related Rules:** PCC-108, PCC-109, PCC-110; ch6 Open-Closed, ch7 Liskov Substitution, ch8 Interface Segregation, ch9 Dependency Inversion.
- **Review Question:** Is this SOLID argument tied to a concrete design consequence rather than to the letter of a rule?

#### PCC-105
**Give each class one coherent responsibility**
- **Source Chapter:** 5 — Applying the Single Responsibility Principle (section: Understanding the Single Responsibility Principle)
- **Principle:** A class should have one coherent responsibility, meaning the coherent purpose it serves. The common alternative wording: no more than one reason to change. Both are useful, but "responsibility" needs careful interpretation.
- **Problem:** A class serving several purposes carries the costs listed in PCC-107 (Table 5.2).
- **Detection Signals:**
  - Methods serving unrelated purposes in one class.
  - Fields used only by a subset of the methods.
  - A class whose name no longer covers all of its members.
- **Recommended Action:** Identify the purposes the class serves and separate the unrelated ones (PCC-107). Validate the split with PCC-108 and PCC-114.
- **Exceptions/Trade-offs:** One responsibility can span many methods (PCC-109) and several orchestrated steps (PCC-110).
- **Related Rules:** PCC-106–PCC-110; PCC-068 (method-level analogue); ch11 Designing smaller classes.
- **Review Question:** What single purpose does this class serve?

#### PCC-106
**Resist growing classes by accumulation**
- **Source Chapter:** 5 — Applying the Single Responsibility Principle (chapter introduction)
- **Principle:** Extra responsibilities usually arrive without anyone deciding. A new requirement goes into an existing, roughly related class because that is the easiest place. Small additions add up until the class does several unrelated things and a change to one breaks the others.
- **Problem:** Unplanned coupling between unrelated behaviors, and fragility under change.
- **Detection Signals:**
  - A change adds a method whose purpose differs from the class's name.
  - A class starts importing namespaces from an unrelated area (persistence, formatting, UI, I/O).
  - The class name no longer describes everything it contains.
- **Recommended Action:** Place a new requirement in its own class, or in the class whose purpose it matches, rather than the nearest convenient one.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-105, PCC-112, PCC-113.
- **Review Question:** Is this new code added here because it belongs here, or only because this class was nearby?

#### PCC-107
**Separate independent responsibilities into dedicated, precisely named classes**
- **Source Chapter:** 5 — Applying the Single Responsibility Principle (sections: Why combining responsibilities creates problems; Separating the responsibilities; Table 5.2)
- **Principle:** When a class combines independent responsibilities, move one into its own class and rename the original after the responsibility it keeps. Use the two together where the application needs both. Book example: `PersonalDataAccess` read people from SQL *and* formatted a person as text, and was split into `PeopleDataReader` and `PersonalDataFormatter`.
- **Problem:** Table 5.2 lists the consequences of combining:
  - Forced dependency: a client needing one job depends on the class holding both.
  - Reduced flexibility: changing the data source means editing the class that also formats.
  - Multiplied class combinations.
  - Harder maintenance: any change marks the whole class as changed, so broader review, retesting and redeployment.
  - Hidden interaction risk: unrelated methods may share state or depend on each other by accident.
  - Less focused tests.
  - Poor names, either unwieldy or vague.
  - More merge conflicts.
- **Detection Signals:**
  - One class referencing both persistence or I/O APIs (`SqlConnection`, `File`) and presentation or formatting logic.
  - Clients that use only one subset of the public methods.
  - Fields shared between methods that serve unrelated purposes.
  - Tests for one behavior that must set up dependencies of the other.
- **Recommended Action:**
  1. Extract the second responsibility into a new class.
  2. Rename the original class to its remaining purpose.
  3. Compose the two at the call site.

  The benefit is not the split itself: each class gets a precise name, independent reuse, focused tests, and changes that do not drag unrelated behavior along.
- **Exceptions/Trade-offs:** Verify the split produces independent types (PCC-114); do not split cohesive operations (PCC-109).
- **Related Rules:** PCC-108, PCC-113, PCC-114, PCC-116; ch8 Interface Segregation (forced dependencies); ch13 Balancing coupling, cohesion, and reuse.
- **Review Question:** Would a client that needs only one of these behaviors be forced to depend on the other?

#### PCC-108
**Test class boundaries with "one reason to change"**
- **Source Chapter:** 5 — Applying the Single Responsibility Principle (section: Using one reason to change as an SRP test)
- **Principle:** List the independent axes along which requirements could change the class.
  - `PersonalDataAccess` had at least two: the data source (SQL to Excel) and the textual format (for example, dropping the ID). After the split, each class responds to one axis.
  - `List<T>` passes the test: faster `Add` or stricter `Remove` error handling are changes to the same responsibility.
- **Problem:** Several independent axes of change in one class mean that unrelated requirement changes collide in it.
- **Detection Signals:**
  - Change history where unrelated requirement changes edit the same class.
  - Plausible future changes (data source, format, rules) that would each force edits to it.
- **Recommended Action:** Split along the independent axes, so each class changes only when its own kind of requirement changes.
- **Exceptions/Trade-offs:** Do not count changes that belong to collaborators. Switching databases is the repositories' reason to change, not `UserAuthorizer`'s; the authorizer's code stays untouched. Its one reason is the authorization flow (for example, adding an "is administrator" check).
- **Related Rules:** PCC-107, PCC-110, PCC-111, PCC-115.
- **Review Question:** How many independent kinds of requirement change would force edits to this class?

#### PCC-109
**Don't measure responsibility by method count; keep operations that serve one purpose together**
- **Source Chapter:** 5 — Applying the Single Responsibility Principle (section: A single responsibility does not mean a single method)
- **Principle:** Many public methods are compatible with a single responsibility, provided every one of them serves the same purpose.
  - `List<T>` (`Add`, `Remove`, `Clear`, `Sort`) provides a dynamic collection.
  - `PeopleRepository` (add, update, delete, get by id, get all) manages persistence of `Person` objects.
- **Problem:** Splitting per operation gives classes like `ListItemsAdder`, `ListItemsRemover`, `ListClearer`, `ListSorter`. Such types work on the same data and are useless apart. They also need a way to reach and modify the shared state, which adds coupling and weakens the collection's control over its own invariants.
- **Detection Signals (of over-splitting):**
  - Clusters of tiny `<Thing><Verb>er` classes all operating on the same data.
  - Classes exposing internal state so that sibling classes can function.
  - Review demands to split a class because it has "more than one public method".
- **Recommended Action:** Judge by purpose, not by method count. Merge operations that serve one purpose on one piece of data back into a cohesive type.
- **Exceptions/Trade-offs:** If the methods actually serve different purposes, PCC-107 applies.
- **Related Rules:** PCC-104, PCC-114, PCC-115; ch13 Balancing coupling, cohesion, and reuse.
- **Review Question:** Do these methods all serve the same purpose on the same data, so that splitting them would only add coupling?

#### PCC-110
**Recognise orchestrators that delegate detailed work as having one responsibility**
- **Source Chapter:** 5 — Applying the Single Responsibility Principle (section: Orchestration can still be one responsibility)
- **Principle:** An orchestrator's methods may call several collaborators and still have one clear purpose. SRP asks whether the operations belong to one responsibility, not whether there is only one operation. Book example: `UserAuthorizer.IsAuthorized` gets the user from a users repository, gets allowed actions from an actions repository, and checks for a match. That is three steps but one responsibility (deciding authorization), because data access is delegated.
- **Problem:** Calling such a class a violation leads to pointless splitting.
- **Detection Signals:**
  - **Legitimate orchestrator:** holds collaborators and composes their calls with a little decision logic.
  - **Real violation signal:** the orchestrator itself performs the collaborators' detailed work (inline SQL, parsing, formatting) instead of delegating.
- **Recommended Action:** Keep the orchestrator. Make sure detailed work stays inside the collaborators.
- **Exceptions/Trade-offs:** The book notes the class will be decoupled further through interfaces in later chapters (DIP). Concrete delegation is already enough for SRP.
- **Related Rules:** PCC-068 (orchestrating method), PCC-108; ch9 Dependency Inversion.
- **Review Question:** Does this class only coordinate its collaborators, or does it also do their detailed work?

#### PCC-111
**Notice who requests changes to a class**
- **Source Chapter:** 5 — Applying the Single Responsibility Principle (section: A practical SRP checklist; Tip)
- **Principle:** When requests to change a class repeatedly originate from two people who own separate business areas, the class probably answers to two reasons for change.
- **Problem:** Requirements from separate business areas collide in one class.
- **Detection Signals:**
  - Issues or commits touching the same class driven by different stakeholders or business areas.
- **Recommended Action:** Split the class along those ownership lines, then re-check with PCC-108 and PCC-114.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-108.
- **Review Question:** Do change requests for this class come from more than one business area?

#### PCC-112
**Be able to describe the class's responsibility in one brief sentence**
- **Source Chapter:** 5 — Applying the Single Responsibility Principle (section: A practical SRP checklist; Table 5.3)
- **Principle:** If the natural description of a class turns into "does X and Y", it may contain unrelated responsibilities.
- **Problem:** A compound description is evidence that unrelated purposes were packaged together.
- **Detection Signals:**
  - Class summaries or doc comments joining two activities with "and".
  - Inability to write a short summary at all.
- **Recommended Action:** Split along the "and", or find the single purpose and rename.
- **Exceptions/Trade-offs:** The signal says "may"; an orchestrator described as coordinating steps can still be one responsibility (PCC-110).
- **Related Rules:** PCC-069 (AND/OR in method names), PCC-110, PCC-113.
- **Review Question:** Can you state this class's job in one short sentence without "and"?

#### PCC-113
**Treat vague or compound class names as SRP warning signs**
- **Source Chapter:** 5 — Applying the Single Responsibility Principle (sections: Table 5.2 "Poorer naming"; A practical SRP checklist, Table 5.3)
- **Principle:** Needing vague words such as *Manager*, or compound names like `PeopleDataReaderAndFormatter`, to name a class can signal accumulated responsibilities. After a good split, names become precise (`PeopleDataReader`, `PersonalDataFormatter`).
- **Problem:** Compound names are unwieldy and vague names (`PeopleManager`) say nothing. Both hide what the class is for.
- **Detection Signals:**
  - Class names ending in `Manager`.
  - Names containing `And`.
  - ch2 also lists `Handler`, `Data`, `Info` as frequent vague words.
- **Recommended Action:** Identify the precise responsibility, split if there are several, and rename.
- **Exceptions/Trade-offs:** It is a signal, not proof. ch2 notes such words are sometimes useful.
- **Related Rules:** PCC-057, PCC-107, PCC-112; ch2 Meaningful names.
- **Review Question:** Does the class name state a precise responsibility, without "Manager" or "And"?

#### PCC-114
**Judge a split by its result; revert splits that leave the new classes tightly coupled**
- **Source Chapter:** 5 — Applying the Single Responsibility Principle (section: A practical SRP checklist, Table 5.3; refactoring experiment)
- **Principle:** A good split usually produces independent, focused, well-named types. Try the split as a refactoring experiment:
  - If it yields two clean, independent abstractions, the original boundary was probably too broad.
  - If it feels artificial and the new classes need extensive knowledge of each other's internals, revert it and reconsider the responsibility.
- **Problem:** Artificial splits add coupling without adding clarity.
- **Detection Signals:**
  - After a split, classes passing internal state back and forth.
  - New public members created only so a sibling can reach internals.
  - One class unusable without the other.
- **Recommended Action:** Keep the split if the types are independent; otherwise revert and rethink where the responsibility boundary lies.
- **Exceptions/Trade-offs:** Constant need for each other's internals indicates the behavior belongs together.
- **Related Rules:** PCC-107, PCC-109; ch13 Balancing coupling, cohesion, and reuse.
- **Review Question:** Can each new class be understood, used and tested without knowing the other's internals?

#### PCC-115
**Don't use class size as the SRP criterion**
- **Source Chapter:** 5 — Applying the Single Responsibility Principle (section: Why combining responsibilities creates problems)
- **Principle:** The costs of mixed responsibilities do not stem from line count. They arise when two independent reasons for change are packaged into one class.
- **Problem:** Judging by size misses small classes that mix concerns, and over-splits large but cohesive ones.
- **Detection Signals:**
  - Split requests justified only by length.
  - Approvals of small classes that still combine unrelated concerns.
- **Recommended Action:** Evaluate by reasons to change (PCC-108) and purpose (PCC-105).
- **Exceptions/Trade-offs:** Size can still prompt a closer look, as the method-level warning thresholds do (PCC-067).
- **Related Rules:** PCC-067, PCC-108, PCC-109; ch11 Designing smaller classes.
- **Review Question:** Is this SRP concern about mixed reasons to change, rather than about size?

#### PCC-116
**Keep independent axes of variation in separate classes so variants add up instead of multiplying**
- **Source Chapter:** 5 — Applying the Single Responsibility Principle (section: Why combining responsibilities creates problems; jacket-and-backpack analogy; Table 5.2 "More combinations")
- **Principle:** Three jackets and three backpacks kept separate are six items. Sewn together they are nine, and each new jacket adds three more. Likewise, once two responsibilities are fused, a class is needed for every combination, so the class count multiplies instead of adding up. The analogy also illustrates lost flexibility: you cannot take one without the other.
- **Problem:** Combinatorial growth of classes, and the inability to vary one aspect independently.
- **Detection Signals:**
  - Families of classes whose names combine two variation axes (`Sql…TextFormatter`, `Excel…TextFormatter`, `Sql…JsonFormatter`).
  - Near-duplicate classes that differ along two dimensions.
- **Recommended Action:** Separate each axis into its own class and compose them at the point of use.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-107; ch6 Open-Closed.
- **Review Question:** Would adding one new variant here require creating several combined classes?

### Code smells & anti-patterns catalogue

| Name | Signal | Rule IDs |
|---|---|---|
| Mechanical SOLID policing | principle cited with no concrete consequence | PCC-104 |
| Accumulated class | new member unrelated to the class name; foreign-area imports | PCC-106 |
| Mixed responsibilities (e.g. data access + formatting) | `SqlConnection` and string formatting in one class; clients using half of the API | PCC-105, PCC-107 |
| Forced dependency | a client needing one behavior must reference a class that also holds another | PCC-107 |
| Hidden interaction risk | unrelated methods sharing fields | PCC-107 |
| Multiple axes of change | unrelated requirement changes edit the same class | PCC-108 |
| Multiple requesters | change requests from different business areas | PCC-111 |
| Over-splitting by operation | `ListItemsAdder`/`ListItemsRemover`-style classes on shared data | PCC-109 |
| Misjudged orchestrator | delegating coordinator flagged as a violation | PCC-110 |
| Orchestrator doing collaborators' work | inline SQL or parsing inside a coordinator | PCC-110 |
| "X and Y" description | class summary needs "and" | PCC-112 |
| Vague or compound class name | `*Manager`, `*AndFormatter` | PCC-113 |
| Artificial split | new classes need each other's internals | PCC-114 |
| Size-based SRP judgment | split or approve justified by line count only | PCC-115 |
| Combinatorial class family | class per combination of two variation axes | PCC-116 |

### Refactoring techniques named in the chapter
- Extract the second responsibility into a dedicated class (formatting moved into `PersonalDataFormatter`).
- Rename the remaining class after its remaining responsibility (`PersonalDataAccess` → `PeopleDataReader`).
- Compose the separated classes at the use site when both are needed.
- Delegate detailed work to collaborators (repositories) and keep the class as an orchestrator.
- Refactoring experiment: try the split, keep it if it yields clean independent abstractions, revert it if the new classes are tightly coupled.
- Apply the Table 5.3 checklist questions as a review procedure.

### Things the author says NOT to do mechanically
- Do not treat SOLID as rules that guarantee good design; they are vocabulary and reasoning aids.
- Do not equate one responsibility with one method, and do not count public methods. `List<T>` and repositories legitimately have many.
- Do not split a cohesive type per operation; it adds coupling and weakens invariants.
- Do not flag an orchestrator as a violation because it performs several delegated steps.
- Do not count a collaborator's reason to change (such as a database switch) as the orchestrator's reason.
- Do not judge SRP by line count; the problem is independent reasons to change packaged together.
- Do not keep a split that feels artificial; revert it and reconsider the boundary.
- Do not treat "Manager"-style names as proof; they are signals.

---

## Chapter 6 — Applying the Open-Closed Principle

### Chapter summary

- Change is the normal state of a codebase, not an exception: requirements arrive piecemeal, get revised or reversed, and released products keep evolving. Design therefore has to keep the impact of change contained.
- OCP: modules, classes and methods should be open for extension and closed for modification. In practice: prefer designs where new behaviour is introduced by adding code rather than by repeatedly editing code that already works.
- Modifying existing code is not wrong in itself but carries risk — other parts of the application or outside consumers depend on it, so edits can introduce defects, surprise people relying on the current behaviour, or break backward compatibility. Automated tests lower that risk; they do not remove it.
- The enabling mechanisms are abstraction and polymorphism: code written against an interface works with any implementation honouring the contract, so new implementations can be added without rewriting dependents.
- Worked example: a user class that branches on an account-type enum to decide which videos are available. Adding a "Kids" account forced edits to the enum, the video data and the branching method itself. Refactoring moves each rule into its own account class behind one interface; the user class only delegates, and a new account is a new class.
- OCP is a design goal, not a guarantee that code never changes: concrete-type selection still changes (contain it in a small factory), contracts sometimes must change, performance must not be sacrificed for formal compliance, radical domain changes justify redesign, and bugs are simply fixed.
- SRP and OCP reinforce each other but answer different questions; neither may be applied mechanically. Good design comes from identifying real responsibilities, real variation and the likely cost of change.

### Rules

#### PCC-117
**Program consumers against the contract, not the implementation**
- **Source Chapter:** 6 — Applying the Open-Closed Principle (section: Reviewing abstract types and polymorphism)
- **Principle:** An abstract type (in C#, typically an interface) states a contract independent of any implementation. Code that holds an object through that abstraction may rely only on the operations the abstraction declares, never on what is specific to the object behind it. This is the foundation that lets new implementations be added later without rewriting dependents.
- **Problem:** A consumer that leans on implementation-specific members can only work with that one implementation; every new variant forces the consumer to be rewritten.
- **Detection Signals:**
  - A variable/parameter typed as an interface that is then cast back to a concrete class (`(Concrete)x`, `x as Concrete`, `x is Concrete c`) to reach extra members.
  - Consumer code calling members that exist only on one implementation.
  - Parameters typed as a concrete class even though the method only uses operations an available abstraction already declares (the book's illustration: a method accepting a collection interface works with any conforming collection).
- **Recommended Action:** Type fields and parameters by the abstraction; confine calls to declared members. If a needed operation is missing, decide whether it belongs in the contract (see PCC-122) instead of casting.
- **Exceptions/Trade-offs:** The chapter's caution against abstractions for imaginary variation applies (PCC-123). Otherwise (not stated in book).
- **Related Rules:** PCC-118, PCC-129, PCC-136, PCC-154.
- **Review Question:** Does this consumer use only members declared by the abstraction it receives, with no cast to a concrete type?

#### PCC-118
**Add new behaviour as new implementations when a family of behaviours is expected to grow**
- **Source Chapter:** 6 — Applying the Open-Closed Principle (section: Understanding the Open-Closed Principle)
- **Principle:** When a predictable family of behaviours is expected to grow, put it behind a stable abstraction so each new member is added as a new implementation rather than by reopening code that works.
- **Problem:** Each edit to working code can introduce defects, surprise developers who depend on current behaviour, or break backward compatibility for consumers inside or outside the application. Tests reduce but do not eliminate this risk.
- **Detection Signals:**
  - The same class/method is modified every time "another kind of X" is requested (visible in history: one method touched by each variant-adding change).
  - Adding a variant requires editing code that also serves all the unrelated existing variants.
  - Shared or externally consumed code changed just to add a new case.
- **Recommended Action:** Identify the axis that varies, introduce (or extend) an abstraction for it, implement each variant as its own type, and make dependents call the abstraction.
- **Exceptions/Trade-offs:** The author explicitly says OCP must not be read as a ban on editing existing code. Applies to predictable, growing families — not to every imaginable change (PCC-123). Practical limits in PCC-120–PCC-126.
- **Related Rules:** PCC-119, PCC-123, ch13 YAGNI/KISS (by name), ch15 testability.
- **Review Question:** Will the next variant of this behaviour be added by writing a new type rather than editing this one?

#### PCC-119
**Replace type-code conditionals in behaviour with polymorphic implementations**
- **Source Chapter:** 6 — Applying the Open-Closed Principle (sections: Recognizing an OCP violation in account behavior; Refactoring account behavior behind an abstraction)
- **Principle:** Behaviour that branches on a type discriminator (an enum value, a type string) belongs in one implementation per type behind a shared interface. The class that used to branch receives the abstraction and delegates to it.
- **Problem:** Every new category extends the same conditional, and in a larger system equivalent branching tends to be repeated in many places — so each new type pushes edits into existing behaviour and the code is not closed for modification.
- **Detection Signals:**
  - `if (x.Kind == Kind.A) … else if (x.Kind == Kind.B)` or `switch (x.Type)` inside a business method that chooses *behaviour* (not object creation).
  - The same enum comparison repeated across several methods or files (grep the enum member name).
  - Adding an enum member forces edits in several methods plus the related data structures.
  - A class holding both a type flag and the collaborators that every variant needs, then filtering/acting differently per flag.
- **Recommended Action:** (1) Define an interface for the behaviour shared by the variants; (2) give each variant its own class implementing only its rule; (3) inject the interface into the former owner and delegate; (4) add future variants as new classes. Side benefit noted by the author: each rule becomes easier to test in its own focused type.
- **Exceptions/Trade-offs:** One selection point still has to map the discriminator to a class (PCC-121), and the enum may still need a localized edit (PCC-120). Do not introduce the hierarchy when the variation is not real (PCC-123).
- **Related Rules:** PCC-120, PCC-121, PCC-136, PCC-163, ch05 SRP.
- **Review Question:** Is there a conditional on a type code that will need another branch for each new variant?

#### PCC-120
**Judge an OCP refactoring by whether existing implementations stay closed, not by "zero edits"**
- **Source Chapter:** 6 — Applying the Open-Closed Principle (section: Refactoring account behavior behind an abstraction)
- **Principle:** A polymorphic design is still a real improvement when a small, localized edit remains (the author's example: adding the new member to the account-type enum). What matters is that the new behaviour arrives through a new type without reopening the existing implementations or their consumer.
- **Problem:** Treating any remaining edit as failure leads either to rejecting a good refactoring or to contortions to avoid a one-line change.
- **Detection Signals:**
  - Healthy: a "new variant" change consists of a new class plus a one-line enum addition (and the factory, PCC-121), with no edits to existing variant classes or the consumer.
  - Unhealthy: the "localized" edit is in fact repeated in many places — that is PCC-119/PCC-121 again.
- **Recommended Action:** Accept a single localized edit; check that the diff for a new variant leaves existing variant classes and consumers untouched.
- **Exceptions/Trade-offs:** (not stated in book) beyond the localization condition above.
- **Related Rules:** PCC-121, PCC-127.
- **Review Question:** When this variant was added, did any existing variant class or consumer have to change?

#### PCC-121
**Contain unavoidable concrete-type selection in one small factory**
- **Source Chapter:** 6 — Applying the Open-Closed Principle (section: Containing unavoidable changes in a factory)
- **Principle:** Even with polymorphism, something must map a choice (e.g. what a user picked at registration) to a concrete type. Put that mapping in one explicit factory or composition boundary; if a modification cannot be eliminated, contain it.
- **Problem:** Without containment, creation conditionals scatter across the codebase and each new type means hunting down and editing all of them. A small explicit factory is easier to reason about.
- **Detection Signals:**
  - `new VariantA(...)` / `new VariantB(...)` inside if/switch blocks in more than one class.
  - Creation logic embedded in business methods instead of a dedicated creator.
  - Several call sites that each decide which concrete class to instantiate for the same family.
- **Recommended Action:** Move the selection into a dedicated factory (a switch over the discriminator returning the abstraction; the book's example rejects an unrecognised value by throwing in the default branch). All callers obtain instances through the factory.
- **Exceptions/Trade-offs:** The factory itself is not closed for modification — it changes whenever a variant is added, and the author accepts that as the right place for the change.
- **Related Rules:** PCC-120, PCC-141, PCC-159, PCC-160.
- **Review Question:** Is the concrete-type decision for this family made in exactly one place?

#### PCC-122
**Change the contract openly when a requirement changes what every implementation needs**
- **Source Chapter:** 6 — Applying the Open-Closed Principle (section: When the abstraction itself must change)
- **Principle:** When a new requirement introduces an input or outcome the shared contract cannot express (the author's example: availability now depends on the user's region, but the method takes no parameters), update the abstraction deliberately and then every implementation. No abstraction can anticipate every future requirement.
- **Problem:** Pretending the new requirement was already modelled pushes it into workarounds around the contract (see PCC-124) or hidden channels, which obscure the real design.
- **Detection Signals:**
  - A new cross-cutting requirement threaded through side channels (global/static state, extra setters, a post-processing helper) to avoid touching an interface signature.
  - Implementations that need information their method signature does not carry.
- **Recommended Action:** Modify the contract (e.g. add the parameter), update each implementation, and use the experience to decide whether the abstraction should evolve further.
- **Exceptions/Trade-offs:** This is a genuine modification and the design is not closed against this kind of change — the author accepts that. Contrast with PCC-156: there one implementation's detail must *not* leak into the contract; here the domain itself changed for every implementation.
- **Related Rules:** PCC-123, PCC-124, PCC-156.
- **Review Question:** Is this new requirement expressed explicitly in the contract rather than smuggled around it?

#### PCC-123
**Abstract only variation that is real now or reasonably likely**
- **Source Chapter:** 6 — Applying the Open-Closed Principle (sections: When the abstraction itself must change; Understanding the practical limits of OCP; Bringing SRP and OCP together)
- **Principle:** Design around variations that are currently meaningful and reasonably likely. The useful question is where change should be contained and whether the abstraction reflects real variation in the domain — not how to make everything extensible.
- **Problem:** Trying to predict every possible variation produces abstractions more complicated than the actual problem, which may still fail to match the requirement that eventually arrives. Forcing every requirement into an extension-only design can cost more than it delivers.
- **Detection Signals:**
  - Extension points, plug-in hooks or strategy layers justified only by "we might need it", with no second variant and no stated expected growth.
  - Generic parameters or options with no current caller using them.
  - An abstraction whose shape did not fit the requirement that actually came next (a sign it was guessed).
- **Recommended Action:** Ask whether the abstraction reflects real domain variation. Abstract what varies now or is reasonably likely; when a new axis of change actually appears, refactor carefully at that point.
- **Exceptions/Trade-offs:** Where a predictable family is clearly growing, the abstraction is warranted (PCC-118). Note the tension with ch09: an interface introduced so a dependency can be swapped or mocked is justified by testability/swapability, not by speculative variants. The term YAGNI is not used in this chapter.
- **Related Rules:** PCC-118, PCC-127, PCC-151, ch13 YAGNI/KISS.
- **Review Question:** Does each extension point correspond to variation that exists or is reasonably likely, rather than merely imaginable?

#### PCC-124
**Do not trade performance or correctness for formal OCP compliance**
- **Source Chapter:** 6 — Applying the Open-Closed Principle (section: Do not sacrifice performance for formal compliance)
- **Principle:** A design that technically avoids changing existing code but introduces a significant performance problem is not an improvement. Clean design must serve the software, not the reverse.
- **Problem:** The author's example avoids changing the account contract by loading every video first and then filtering by region in a separate new class. When the data source could have filtered before materialising, this moves far more data and runs slower.
- **Detection Signals:**
  - A new "filterer"/post-processor class applied after a full load (`GetAll()` then `.Where(...)` elsewhere) introduced to avoid widening a query contract.
  - Materialising (`ToList()`) a whole data set before applying a restriction the data source could have applied itself.
- **Recommended Action:** Prefer the design that meets performance and correctness needs even if it means changing existing code — e.g. pass the restriction to the data source so it is applied before data is materialised.
- **Exceptions/Trade-offs:** The performance argument rests on the source being able to filter on its side; the author frames it as "generally more efficient" in that case.
- **Related Rules:** PCC-122.
- **Review Question:** Was an extension-only workaround chosen at the cost of loading or processing much more data than necessary?

#### PCC-125
**Redesign rather than preserve obsolete abstractions after a radical domain change**
- **Source Chapter:** 6 — Applying the Open-Closed Principle (section: When changing existing code is the right choice)
- **Principle:** When a requirement departs fundamentally from the original model (the author's example: a platform drops account-based access in favour of free and pay-per-view videos), redesign or replace the affected modules instead of keeping the old hierarchy just to avoid modification.
- **Problem:** Retaining obsolete concepts merely to stay "closed" embeds them in the design and produces convoluted code.
- **Detection Signals:**
  - Types, enums or hierarchies that no longer correspond to any current domain concept but are still adapted and extended.
  - Adapters whose sole purpose is to translate a new model into an abandoned hierarchy.
- **Recommended Action:** Refactor or redesign the affected modules — provided the change is planned, tested and protected by version control.
- **Exceptions/Trade-offs:** The redesign must be planned, tested and under version control (the author's stated conditions).
- **Related Rules:** PCC-122, PCC-126.
- **Review Question:** Does the design still carry abstractions for concepts the domain has abandoned?

#### PCC-126
**Fix defects in place; never wrap them in a "corrected" subtype**
- **Source Chapter:** 6 — Applying the Open-Closed Principle (section: When changing existing code is the right choice)
- **Principle:** If existing code is wrong, correct it. Do not add a parallel subtype that copies the old behaviour minus the defect just to claim the original stayed closed for modification.
- **Problem:** Formalism wins over correctness: a near-duplicate type appears, and the defective original remains in place.
- **Detection Signals:**
  - A bug-fix change that adds a subclass/implementation instead of editing the faulty code.
  - An override that is a near copy of the base implementation differing only by the fix.
- **Recommended Action:** Edit the defective code directly (and cover it with tests).
- **Exceptions/Trade-offs:** (not stated in book) — the author presents this as the clearest case for modification.
- **Related Rules:** PCC-125.
- **Review Question:** Was this bug fixed in the code that contained it, rather than by adding a parallel type?

#### PCC-127
**Use SRP and OCP together, and apply neither mechanically**
- **Source Chapter:** 6 — Applying the Open-Closed Principle (sections: Refactoring account behavior behind an abstraction; Bringing SRP and OCP together)
- **Principle:** SRP asks whether a class has one coherent responsibility; OCP asks whether expected variations can be added without repeatedly modifying stable behaviour. Moving each variant's behaviour into its own class typically satisfies both.
- **Problem:** Mechanical application misuses both: splitting every method into its own class misuses SRP; creating abstractions for every imaginable requirement misuses OCP.
- **Detection Signals:**
  - Swarms of tiny classes created by reflexive splitting, with no distinct responsibility or reason to change.
  - Abstraction layers with no variation behind them.
- **Recommended Action:** Identify real responsibilities, real variation and the likely cost of change first; then apply the principles where they pay off.
- **Exceptions/Trade-offs:** This rule *is* the trade-off guidance.
- **Related Rules:** PCC-123, PCC-149, ch05 SRP.
- **Review Question:** Is each new class or abstraction justified by a real responsibility or real variation rather than by rule-following?

### Code smells & anti-patterns catalogue

| Name | Signal | Rule IDs |
|---|---|---|
| Type-code conditional in behaviour | `if`/`switch` on an enum or type string inside a business method selecting behaviour | PCC-119 |
| Duplicated type branching | Same enum comparison appears in several methods/files | PCC-119, PCC-121 |
| Scattered creation conditionals | `new VariantX()` inside if/switch in multiple classes | PCC-121 |
| Cast-back from abstraction | Interface reference cast/`as`/`is` to a concrete class to reach extra members | PCC-117 |
| Hot-spot method | Same method edited for every new variant | PCC-118 |
| Contract smuggling | New requirement routed through statics/setters/post-processors to avoid changing a signature | PCC-122 |
| Formal-compliance performance hack | Load everything, then filter in a new class to avoid changing a contract | PCC-124 |
| Obsolete abstraction kept alive | Hierarchy/enum for concepts the domain no longer has, plus adapters into it | PCC-125 |
| Bug-preserving parallel subtype | New subclass that copies the base minus a defect | PCC-126 |
| Speculative abstraction | Extension points for imagined requirements, no second variant | PCC-123, PCC-127 |
| Mechanical splitting | One-method classes produced by reflexive SRP | PCC-127 |

### Refactoring techniques named in the chapter

- Replace a type-code conditional with polymorphism: interface for the shared behaviour, one class per variant, owner delegates through a constructor-supplied abstraction.
- Isolate concrete-type selection in a dedicated factory (switch over the discriminator, one return per variant, unknown value rejected).
- Deliberately evolve the contract (add a parameter) and update all implementations when the domain gains a new axis.
- Push filtering to the data source rather than filtering after materialisation.
- Redesign/replace modules when the domain model changes radically (planned, tested, version-controlled).
- Fix bugs directly in the defective code.
- Pragmatic decision table (paraphrase of the chapter's table):
  - growing, predictable family of behaviours → introduce or extend an abstraction, add implementations;
  - concrete-type selection must change → isolate it in a small factory or composition boundary;
  - a requirement changes the shared contract → update the abstraction on purpose;
  - extension-only design is costly in performance → choose the design meeting performance and correctness, even if code changes;
  - radical domain change → refactor or redesign rather than keep obsolete abstractions;
  - a bug → modify the code and fix it.

### Things the author says NOT to do mechanically

- Do not read OCP as a prohibition on ever editing existing code.
- Do not demand a fully modification-free design; a localized edit (enum member, factory branch) is acceptable.
- Do not try to predict every future variation and build abstractions for all of them.
- Do not pretend a new requirement was already modelled to avoid changing a contract.
- Do not accept a design that satisfies OCP on paper but harms performance.
- Do not keep an obsolete hierarchy alive just to avoid modification after a radical domain change.
- Do not "fix" a bug by adding a parallel subtype.
- Do not split every method into a class (SRP misuse) or abstract every imaginable requirement (OCP misuse).

---

## Chapter 7 — Applying the Liskov Substitution Principle

### Chapter summary

- Deriving from a type is a promise: whatever the base type says it can do, the derived type says it can do too, and code written against the base type is entitled to rely on that.
- The compiler checks only half of the promise — signatures. A subtype can compile cleanly yet throw where the base returned a value, or reject inputs the base accepted; the hierarchy looks fine and callers pay (type checks, try/catch wrappers, special handling of one subtype).
- LSP: an object of a derived type must be usable wherever the base type is expected without errors, invalid results or subtype-specific handling; consumers should not need to know which subtype they got. Inheritance is a contract about behaviour, not about method signatures.
- Violations can originate either in the subtype or in an abstraction that promises too much (birds that must fly vs. an ostrich; a square forced to keep independent width/height setters; every plane assumed to carry fuel vs. a toy plane).
- Remedies: type consumers by the capability they need (a "flyable" capability rather than "bird"), keep the base type to what is meaningful for all members, extract optional capabilities into interfaces, move shared capability logic into a service, move legitimate special handling into the subtype, and split capabilities when an operation is invalid for some types.
- Contract discipline: a subtype must not weaken postconditions (e.g. return null where an object is guaranteed) or strengthen preconditions (e.g. a stricter threshold).
- When inheritance only varies a configuration value, collapse it into one type configured by value, with creation centralised in a factory.

### Rules

#### PCC-128
**Treat inheritance as a behavioural contract, not a signature match**
- **Source Chapter:** 7 — Applying the Liskov Substitution Principle (sections: chapter introduction; Understanding the Liskov Substitution Principle)
- **Principle:** A subtype must be usable anywhere its base type is expected without causing errors, invalid results or a need for subtype-specific handling. Matching signatures and compiling is necessary but not sufficient; the subtype may specialise *how* it fulfils the contract but must not invalidate the base type's promises.
- **Problem:** A hierarchy that compiles but does not keep its promises shifts the cost to callers, who start checking concrete types, wrapping calls in try/catch, or treating one subtype differently.
- **Detection Signals:**
  - Callers of a base-typed reference wrapping the call in `try { … } catch` specifically to cope with some subtypes.
  - Client code that inspects the runtime type before using a base reference (see PCC-136).
  - Overrides that change the observable outcome category (value vs. exception vs. invalid value) compared with the base.
- **Recommended Action:** For each override, state what the base type promises (valid inputs, guaranteed outputs, no-throw expectations) and verify the subtype honours all of it; if it cannot, redesign the hierarchy (PCC-130, PCC-134, PCC-138, PCC-141).
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-129, PCC-132, PCC-133, PCC-136, PCC-145.
- **Review Question:** Could every subtype be passed to every caller of the base type without the caller noticing a difference in validity of results or failures?

#### PCC-129
**Consumers assume only what the base type promises; subtypes keep every promise and convention**
- **Source Chapter:** 7 — Applying the Liskov Substitution Principle (section: Understanding the Liskov Substitution Principle — "LSP in practice" note)
- **Principle:** Code consuming a base type should rely on nothing beyond what that base type promises, and every subtype must honour those promises and conventions. A base type defines what callers are allowed to assume.
- **Problem:** If either side breaks this — consumers leaning on unpromised behaviour, or subtypes dropping promised behaviour — substitution fails at run time.
- **Detection Signals:**
  - Consumers depending on behaviour only one subtype provides.
  - Subtypes that silently ignore, no-op or change the meaning of an inherited member.
- **Recommended Action:** Make the base contract explicit (documented guarantees), align consumers to it, and audit each subtype against it.
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-117, PCC-128.
- **Review Question:** Does this consumer rely only on what the base type guarantees, and does every subtype deliver all of it?

#### PCC-130
**Type the consumer by the capability it needs; keep non-universal capabilities off the base type**
- **Source Chapter:** 7 — Applying the Liskov Substitution Principle (section: A memorable substitutability analogy)
- **Principle:** If a consumer needs "things that can do X", its parameter should be a capability abstraction for X, not a broad family type. A base type should not declare a capability that not all of its members have (the author's analogy: a bird base type with a fly operation fails as soon as an ostrich is handed over; the right parameter is a "flyable" capability, which kites and drones could also implement).
- **Problem:** A broad base type that declares a capability some members lack guarantees a run-time failure when such a member is used exactly as the base type says it can be.
- **Detection Signals:**
  - A base class/interface declaring an operation that some derived types cannot perform (overrides that throw, no-op or return nonsense).
  - A method whose parameter is a broad family type although it only uses one capability of it.
- **Recommended Action:** Introduce a capability interface for the operation; have only capable types implement it; retype consumers to the capability; remove the operation from the broad base type.
- **Exceptions/Trade-offs:** The broad base type remains useful for treating members uniformly where every member really shares the behaviour — it just should not declare the capability not all have.
- **Related Rules:** PCC-134, PCC-138, PCC-142, PCC-148.
- **Review Question:** Does the base type declare only behaviour that every one of its subtypes can genuinely perform?

#### PCC-131
**Distrust "is-a" hierarchies where the subtype must couple members to protect its own invariant**
- **Source Chapter:** 7 — Applying the Liskov Substitution Principle (section: A memorable substitutability analogy — square/rectangle illustration)
- **Principle:** A mathematical or taxonomic "is-a" relationship does not make a valid subtype. If the base exposes independently settable members (width and height) and the subtype must override them so that setting one changes the other (to keep its "equal sides" invariant), callers relying on independence break.
- **Problem:** Code valid for any base instance (set width, expect height unchanged) yields wrong results for the subtype — the same substitutability failure as the bird example.
- **Detection Signals:**
  - An overridden property setter that also assigns another inherited property.
  - A subtype whose invariant is stricter than the base type's and is enforced by changing inherited mutators.
- **Recommended Action:** (not stated in book beyond identifying the problem; the chapter's general remedies apply — reconsider the inheritance relationship or the base contract, PCC-132/PCC-138.)
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-128, PCC-132, PCC-140.
- **Review Question:** Does any subtype override a mutator so that it changes state the base type promised to leave alone?

#### PCC-132
**A subtype must not produce invalid results from behaviour the base type guarantees**
- **Source Chapter:** 7 — Applying the Liskov Substitution Principle (section: When a subtype breaks the base-type contract)
- **Principle:** Behaviour that the base type appears to guarantee must yield valid results for every subtype. The author's example: an abstract plane computes the percentage of remaining fuel from two overridable fuel values; a toy plane returns zero for both, so the inherited calculation becomes zero divided by zero and returns an invalid number.
- **Problem:** A caller holding only the base type has every right to expect the calculation to work, and is surprised when the object turns out to be the misfit subtype.
- **Detection Signals:**
  - Overrides returning degenerate constants (`0`, `null`, empty) for members that inherited base logic uses in arithmetic or as a divisor.
  - Base-class methods computing from virtual members with no subtype able to make them meaningful.
  - NaN/Infinity/invalid values appearing only for certain subtypes.
- **Recommended Action:** Choose by domain among the author's three remedies: (a) remove the concept (fuel) from the general base contract; (b) stop deriving the misfit type from the base; (c) change the subtype so it supplies meaningful values if the domain truly requires every member to have them. The usual follow-up is PCC-134.
- **Exceptions/Trade-offs:** The right remedy depends on the application domain — the author explicitly leaves the choice open.
- **Related Rules:** PCC-128, PCC-134, PCC-135.
- **Review Question:** Does every subtype give meaningful values to the members that inherited base logic computes with?

#### PCC-133
**A subtype must not throw where the base type returns a result**
- **Source Chapter:** 7 — Applying the Liskov Substitution Principle (sections: chapter introduction; Summary — reinforced by ch08 "When an interface forces meaningless behavior" and ch09 "Identifying the issues")
- **Principle:** An override that throws in situations where the base implementation returns normally breaks the contract, even though it compiles.
- **Problem:** Callers written against the base type do not expect the exception, so the program fails at the first substitution; callers then respond with try/catch or type checks.
- **Detection Signals:**
  - `throw new NotImplementedException()`, `throw new NotSupportedException()` or `throw new InvalidOperationException(...)` inside an `override` or interface implementation where the base/contract implies success.
  - An override whose first statement is a guard that throws for inputs the base accepts (see PCC-140).
- **Recommended Action:** Redesign so the type does not claim the operation (PCC-134/PCC-138/PCC-145), or move the behaviour so no override is needed (PCC-165).
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-128, PCC-140, PCC-145, PCC-164.
- **Review Question:** Does any override throw where the base type would have returned a valid result?

#### PCC-134
**Move capabilities that only some subtypes support into a capability interface**
- **Source Chapter:** 7 — Applying the Liskov Substitution Principle (section: Extracting capabilities into interfaces)
- **Principle:** If both kinds of member are valid in the domain (e.g. fuelled and non-fuelled planes), the base type should hold only behaviour meaningful for every member. Optional capability members move to a separate interface implemented only by types that support it.
- **Problem:** Keeping the optional capability on the base forces non-capable types to fake it, producing invalid results or exceptions.
- **Detection Signals:**
  - Base members that some subtypes override with dummy values or empty bodies.
  - Inherited members that "mean nothing" for some subtypes (author's summary wording paraphrased).
- **Recommended Action:** Extract the capability members into an interface; strip them from the base; have capable subtypes implement both base and capability; retype consumers that need the capability to the interface. The type system then rejects passing a non-capable object at compile time instead of failing later at run time.
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-130, PCC-135, PCC-138, PCC-146.
- **Review Question:** Do all members of the base type make sense for every subtype, with optional capabilities expressed as separate interfaces?

#### PCC-135
**Put logic shared across a capability in a service that accepts the capability interface**
- **Source Chapter:** 7 — Applying the Liskov Substitution Principle (section: Moving shared behavior out of inappropriate subtypes)
- **Principle:** When the same calculation would otherwise be copied into each class implementing a capability, place it in a separate service that takes the capability interface as input. It then works for any capable type (plane, car, boat…) without knowing the concrete type.
- **Problem:** Leaving the logic in an inappropriate base type breaks substitutability; copying it into each implementer duplicates business logic.
- **Detection Signals:**
  - Identical computations duplicated across implementers of the same capability interface.
  - Capability logic lingering in a base class after the capability was extracted.
- **Recommended Action:** Create a small service (calculator) whose method accepts the capability interface and computes from its members; remove the copies.
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-134, ch13 DRY (by name).
- **Review Question:** Is capability-wide logic implemented once, against the capability interface, rather than duplicated or kept in an ill-fitting base?

#### PCC-136
**Remove concrete-subtype checks from client code**
- **Source Chapter:** 7 — Applying the Liskov Substitution Principle (section: Avoiding subtype checks in client code)
- **Principle:** Client code that checks for a specific subtype before using an abstraction signals that the abstraction does not represent its implementations uniformly.
- **Problem:** The client can no longer rely on the abstraction alone; it must know that one concrete type behaves differently. It also creates an OCP problem, because every new exceptional subtype may require another branch in the client.
- **Detection Signals:**
  - `if (item is SpecificType)`, `item as SpecificType`, `item.GetType() == typeof(...)`, pattern-matching `switch` on concrete types, inside loops or methods typed against an abstraction.
  - Special-case adapters or workarounds created in the client for one implementation.
- **Recommended Action:** Decide whether the special behaviour is legitimate for that type. If yes, move it into the type (PCC-137). If no, the type should not implement the abstraction — split the capability (PCC-138). Afterwards the client goes back to treating every element uniformly.
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-119, PCC-137, PCC-138, PCC-163.
- **Review Question:** Does any code holding an abstraction check which concrete type it received?

#### PCC-137
**If the special case is legitimate behaviour, move it inside the subtype**
- **Source Chapter:** 7 — Applying the Liskov Substitution Principle (section: Avoiding subtype checks in client code)
- **Principle:** When the deviation is valid domain behaviour (the author's example: a kerosene lamp needs an adapter before a fuel hose can be attached), the subtype should encapsulate it inside its own implementation of the contract operation.
- **Problem:** Leaving the special handling in the client couples it to one concrete type and multiplies branches as exceptions accumulate.
- **Detection Signals:** Client branches that wrap or adapt one particular implementation before calling the contract method on it.
- **Recommended Action:** Move the adaptation into the subtype's implementation of the contract member; delete the client branch so the loop calls the abstraction uniformly, as its signature promises.
- **Exceptions/Trade-offs:** Only if the behaviour is truly valid for the type; otherwise use PCC-138.
- **Related Rules:** PCC-136, PCC-138.
- **Review Question:** Is type-specific handling encapsulated in the type rather than in its callers?

#### PCC-138
**If the operation is not valid for a type, stop claiming it: split the capability**
- **Source Chapter:** 7 — Applying the Liskov Substitution Principle (section: When a type should not implement the broader interface)
- **Principle:** When an operation is not valid for some type, the accurate design stops asserting that all such objects support it. Split the capabilities: a narrower base capability shared by all (e.g. "holds fuel") and an extended capability (e.g. "can be fuelled by hose", deriving from the narrower one) implemented only by types that support it.
- **Problem:** A broad interface claiming an unsupported operation forces either client special cases or failing implementations.
- **Detection Signals:**
  - An implementer of an interface for which one member is meaningless in the domain.
  - Client code that avoids calling one interface member for certain implementations.
- **Recommended Action:** Introduce a narrower interface for the common concept and an extending interface for the extra operation; re-point each type to the interface it can honour; retype clients accordingly. This anticipates ISP.
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-134, PCC-146, PCC-145.
- **Review Question:** Does every type implement only interfaces whose operations it can genuinely perform?

#### PCC-139
**Do not weaken postconditions**
- **Source Chapter:** 7 — Applying the Liskov Substitution Principle (section: Avoiding weakened postconditions and strengthened preconditions)
- **Principle:** A subtype must not promise callers less than the base contract does. Example given: if the base guarantees a lookup always returns an object, a subtype returning null on "not found" breaks the guarantee.
- **Problem:** Callers written against the base do not check for the weaker outcome because they were told they never need to — so they fail.
- **Detection Signals:**
  - An override returning `null` (or an empty/default value) where the base contract guarantees a real object.
  - Overrides that skip side effects or guarantees the base performs.
- **Recommended Action:** Make the subtype meet the base postcondition, or revisit the hierarchy so the type is not presented as a substitute.
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-128, PCC-140.
- **Review Question:** Does every override deliver at least the outcome the base type guarantees?

#### PCC-140
**Do not strengthen preconditions**
- **Source Chapter:** 7 — Applying the Liskov Substitution Principle (section: Avoiding weakened postconditions and strengthened preconditions)
- **Principle:** A subtype must not require more from callers than the base contract does, nor reject inputs the base accepts. The author's example: a bank account allows withdrawals under a high threshold without extra authorisation, while a children's account subtype lowers that threshold, so a caller's valid expectation for the base type fails for the subtype.
- **Problem:** Callers holding a base reference make requests that are valid for the base and get unexpected rejection or extra requirements.
- **Detection Signals:**
  - Overrides with tighter guards, lower limits or extra validation than the base.
  - Overrides that throw for argument values the base handles (the ch09 report generator rejecting non-developer employees).
- **Recommended Action:** Remove the stricter precondition from the subtype; if the difference is only a value, use PCC-141; if the subtype really handles a narrower input, reconsider the inheritance (PCC-164/PCC-165).
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-133, PCC-139, PCC-141, PCC-164.
- **Review Question:** Does any override reject or demand more of inputs that the base type accepts?

#### PCC-141
**Don't use inheritance merely to vary a configuration value**
- **Source Chapter:** 7 — Applying the Liskov Substitution Principle (section: Avoiding weakened postconditions and strengthened preconditions — refactored account)
- **Principle:** When subtypes differ only by a constant (e.g. an authorisation threshold), keep one type and supply the value as configuration. If only valid configurations should exist, centralise them in a static factory method (with a private constructor) that maps a kind to its permitted value.
- **Problem:** Inheritance used for configuration creates needless subtype relationships and the substitution problems that come with them.
- **Detection Signals:**
  - Subclasses whose overrides are copies of the base method differing only in a literal.
  - A family of subclasses each overriding one numeric/string constant.
- **Recommended Action:** Collapse the hierarchy into one class with a readonly field for the varying value; add a static `Create(kind)` factory (unrecognised kinds rejected) if valid combinations must be controlled; make the constructor private.
- **Exceptions/Trade-offs:** Adding a new kind still changes the factory — the author accepts this as the place where the creation decision belongs, so the rest of the code needs no subtype knowledge (links back to OCP containment).
- **Related Rules:** PCC-121, PCC-140, PCC-160.
- **Review Question:** Do these subclasses differ only by a value that could be passed in as configuration?

### Code smells & anti-patterns catalogue

| Name | Signal | Rule IDs |
|---|---|---|
| Over-promising base type | Base declares an operation some subtypes cannot perform | PCC-130, PCC-134 |
| Degenerate override | Override returns 0/null/empty used by base arithmetic → NaN/invalid result | PCC-132 |
| Throwing override | `NotImplementedException` / `NotSupportedException` / `InvalidOperationException` in an override where the base succeeds | PCC-133 |
| Defensive try/catch around base calls | Callers catch exceptions only some subtypes raise | PCC-128, PCC-133 |
| Runtime type check in client | `is` / `as` / `GetType()` / type-pattern switch on an abstraction | PCC-136 |
| Client-side adapter for one subtype | Special wrapping of one implementation before calling the contract | PCC-137 |
| Coupled-setter override (square/rectangle) | Overridden setter also sets another inherited property | PCC-131 |
| Weakened postcondition | Override returns null/default where base guarantees an object | PCC-139 |
| Strengthened precondition | Override has stricter limit/guard or rejects base-valid input | PCC-140 |
| Inheritance for configuration | Subclasses differing only by a literal | PCC-141 |
| Duplicated capability logic | Same calculation copied into each implementer | PCC-135 |
| Meaningless inherited members | Members that make no sense for some subtypes | PCC-134, PCC-138 |

### Refactoring techniques named in the chapter

- Retype the consumer to a capability interface (e.g. "flyable" instead of "bird").
- Extract capability members from the base type into an interface implemented only by capable types.
- Move capability-wide shared logic into a separate service accepting the capability interface.
- Move legitimate special handling (adapter) from the client into the subtype's implementation.
- Split a capability into a narrower base interface plus an extended interface (interface inheritance).
- Domain-driven choice among: shrink the base contract, drop the inheritance relationship, or fix the subtype.
- Replace configuration-only inheritance with one class + injected value + static factory method with a private constructor.

### Things the author says NOT to do mechanically

- Do not conclude that the broad base type should be deleted — it remains useful for uniform treatment; it just must not declare non-universal capabilities.
- Do not apply one fixed remedy to a broken contract; the right fix (change base, remove inheritance, change subtype) depends on the domain.
- Do not trust "is-a" modelling (mathematical or taxonomic) as proof of substitutability.
- Do not accept a compiling hierarchy as correct — signatures are only half the contract.
- Do not keep type checks or try/catch in callers as a workaround for a subtype that breaks the contract.

---

## Chapter 8 — Applying the Interface Segregation Principle

### Chapter summary

- Interfaces should create useful boundaries; one that collects unrelated operations becomes a liability — implementers must provide meaningless members and clients depend on capabilities they never use.
- ISP: clients of an interface should not be forced to depend on methods they do not use. Shape interfaces around cohesive client needs; small size is a frequent result, not the goal — an interface should be as broad as one coherent capability or client role requires.
- Both sides of the relationship provide evidence: clients reveal when an interface exposes more than they need; awkward or meaningless implementations reveal when a contract is too broad.
- An interface is a client-facing service boundary (bank analogy: a specialised valuables service is exposed through its own role so ordinary customers' interaction does not change).
- Worked example: a personal-data access interface gains a write method that a read-only public census source cannot support; the stub that throws is an LSP failure caused by an ISP problem. Splitting into reader and writer capabilities fixes it; one class may implement both.
- ISP, LSP and SRP reinforce each other but ask different questions; SRP and ISP can diverge (a cohesive stack class may still deserve a push-only role interface).
- Segregation has a cost (more abstractions to name, document and maintain); split only when it clarifies client dependencies or prevents unsupported behaviour. Practical checks help spot interface pollution before it grows.

### Rules

#### PCC-142
**Shape each interface around one coherent capability or client role**
- **Source Chapter:** 8 — Applying the Interface Segregation Principle (section: Understanding the Interface Segregation Principle)
- **Principle:** Clients should not be forced to depend on methods they do not use. Design interfaces around cohesive client needs rather than collecting unrelated operations in one broad contract. An interface should be as broad as necessary to represent one coherent capability or client role — no broader, but size is not itself the goal.
- **Problem:** Broad contracts force implementers to supply meaningless members and couple clients to capabilities they never call.
- **Detection Signals:**
  - An interface whose members fall into clearly separate groups (e.g. read vs. write, domain operation vs. file I/O).
  - Different clients each using a disjoint subset of the interface.
- **Recommended Action:** Group members by the capability/role clients actually need; extract each group as its own interface.
- **Exceptions/Trade-offs:** Do not optimise for the smallest interface; a broader interface is right when it represents one coherent capability (PCC-151).
- **Related Rules:** PCC-143, PCC-146, PCC-151, PCC-130.
- **Review Question:** Does this interface represent exactly one coherent capability or client role?

#### PCC-143
**Use both clients and implementers as evidence that a contract is too broad**
- **Source Chapter:** 8 — Applying the Interface Segregation Principle (section: Understanding the Interface Segregation Principle)
- **Principle:** ISP is phrased from the client's perspective, but both sides count. A client is code that receives/stores an object through the interface and calls its members; an implementer is the concrete type fulfilling it. Client usage shows over-exposure; awkward or meaningless implementations show over-breadth.
- **Problem:** Looking only at one side misses half the warning signs.
- **Detection Signals:**
  - Clients calling only a small fraction of an interface's members.
  - Implementations that feel unrelated to the type, are empty, or throw.
- **Recommended Action:** During review, list each interface's clients and the members each uses, and each implementer's quality of implementation per member; split where either side shows mismatch.
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-145, PCC-148, PCC-152.
- **Review Question:** Do all clients use, and all implementers meaningfully support, every member of this interface?

#### PCC-144
**Add a new capability as a new interface, not as a new member of a widely used one**
- **Source Chapter:** 8 — Applying the Interface Segregation Principle (section: Thinking of an interface as a service boundary)
- **Principle:** Treat an interface as a client-facing service boundary. When a specialised capability is added, expose it through a separate interface and let only the appropriate concrete types implement it, leaving the ordinary interface unchanged (bank analogy: regular customers need not learn the vault's security procedures).
- **Problem:** Adding unrelated methods to a broadly used interface burdens every existing client and implementer with something they don't need.
- **Detection Signals:**
  - A change that adds a member to an interface with many implementers/clients, where only one or a few will use or support it.
- **Recommended Action:** Create a second interface for the new capability; implement it only where it applies; give the new capability's clients that interface.
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-152, PCC-153, PCC-118.
- **Review Question:** Is this new member something every existing client and implementer of the interface actually needs?

#### PCC-145
**Never satisfy an interface with stubs that throw or do nothing meaningful**
- **Source Chapter:** 8 — Applying the Interface Segregation Principle (section: When an interface forces meaningless behavior; closing "Design rule" note)
- **Principle:** Do not add stub implementations just to satisfy an interface. A member that does not fit a type is a signal: either the interface covers too much and should be split, or the interface is fine and this type should not implement it at all.
- **Problem:** A stub (e.g. throwing `NotImplementedException`) satisfies the compiler but not the promise: anyone holding the interface may call the member and get an exception — an LSP violation created by the ISP violation.
- **Detection Signals:**
  - `throw new NotImplementedException();` (or `NotSupportedException`) as the whole body of an interface member implementation.
  - Empty or dummy-return implementations of interface members.
- **Recommended Action:** Ask the two questions: split the interface (PCC-146), or remove the interface from this type (PCC-138). Do not keep the stub.
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-133, PCC-138, PCC-146, PCC-149.
- **Review Question:** Does any type implement an interface member with a stub that throws or does nothing?

#### PCC-146
**Split a broad interface along capabilities**
- **Source Chapter:** 8 — Applying the Interface Segregation Principle (section: Splitting the interface by capability)
- **Principle:** When an interface bundles capabilities as though every implementer supports all of them (e.g. reading and writing personal data), separate them into focused interfaces so each type implements only what it supports.
- **Problem:** The false assumption that all sources support all capabilities means at least one implementation cannot honour the full contract.
- **Detection Signals:**
  - Read-only sources (public APIs, external data) implementing write members.
  - An interface mixing query and mutation members whose implementers differ in what they support.
- **Recommended Action:** Extract one interface per capability (reader / writer); re-declare each implementer with only the capabilities it supports; retype clients to the capability they use.
- **Exceptions/Trade-offs:** See PCC-151 — split when it clarifies dependencies or prevents unsupported behaviour, not by reflex.
- **Related Rules:** PCC-134, PCC-138, PCC-147, PCC-148.
- **Review Question:** Does every implementer of this interface support every capability it bundles?

#### PCC-147
**Let one class implement several focused interfaces**
- **Source Chapter:** 8 — Applying the Interface Segregation Principle (section: Splitting the interface by capability)
- **Principle:** Interface segregation does not require one concrete class per capability. A type that genuinely supports several capabilities (e.g. a text-file source that can both read and write) implements all the corresponding focused interfaces.
- **Problem:** Misreading ISP as "one class per interface" leads to needless class proliferation.
- **Detection Signals:**
  - Classes split apart only to mirror interface splits, duplicating shared state (e.g. the same file path) across them.
- **Recommended Action:** Keep the class whole if its capabilities belong together; declare it as implementing each focused interface.
- **Exceptions/Trade-offs:** Whether the class itself should be split is an SRP question, not an ISP one (PCC-149).
- **Related Rules:** PCC-146, PCC-149.
- **Review Question:** Was a class split only because its interfaces were split, without an SRP reason?

#### PCC-148
**Make each client depend on the narrowest interface it actually uses**
- **Source Chapter:** 8 — Applying the Interface Segregation Principle (section: Splitting the interface by capability)
- **Principle:** Each client should receive the focused interface it needs (e.g. a printer that only reads takes a reader abstraction). Implementers are no longer forced to provide unsupported operations, clients no longer depend on unused methods, and a client cannot call a method that will fail on it because a type that cannot perform the operation does not expose it.
- **Problem:** A client holding a broad interface can call members that may fail and is coupled to changes in members it never uses.
- **Detection Signals:**
  - Constructor/parameter typed with a broad interface while the class calls only one or two of its members.
- **Recommended Action:** Retype the client's dependency to the focused interface; inject it through the constructor.
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-142, PCC-150, PCC-154.
- **Review Question:** Does this client depend on any interface member it never calls?

#### PCC-149
**Diagnose with each principle's own question: SRP, LSP and ISP are distinct**
- **Source Chapter:** 8 — Applying the Interface Segregation Principle (section: How ISP relates to LSP and SRP)
- **Principle:** The three principles reinforce each other but ask different questions. SRP: does this class hold one coherent responsibility (warning sign: unrelated reasons to change combined)? LSP: can every subtype safely stand in for the base (warning sign: throws, invalid results, special-case client logic)? ISP: does this client depend only on the operations it needs (warning sign: members some clients/implementers don't need or can't support)?
- **Problem:** Conflating them produces wrong fixes. An oversized interface that forces a throwing stub is an ISP problem *and* an LSP problem; it may contribute to an SRP problem if a class takes on unrelated capabilities just to satisfy the contract — but that is not automatic.
- **Detection Signals:** Use the three warning signs above as separate checklist items.
- **Recommended Action:** Name which principle each finding violates, and fix at the right level (class split vs. hierarchy change vs. interface split).
- **Exceptions/Trade-offs:** A design that satisfies one principle can still fall short of another.
- **Related Rules:** PCC-145, PCC-150, PCC-127, ch05 SRP.
- **Review Question:** For this finding, is the defect about responsibility (SRP), substitutability (LSP) or client dependency (ISP)?

#### PCC-150
**Offer a narrower role interface even for a cohesive class when clients use only part of it**
- **Source Chapter:** 8 — Applying the Interface Segregation Principle (section: How ISP relates to LSP and SRP — stack example)
- **Principle:** SRP and ISP do not always agree. A stack's push and pop belong to one responsibility, so SRP has no objection; yet a client that only pushes may benefit from a narrower interface without pop — particularly when different client roles need different capabilities.
- **Problem:** Exposing the full surface to every client role couples clients to operations outside their role.
- **Detection Signals:**
  - Distinct client roles of one cohesive class, each using a different subset of members.
- **Recommended Action:** Define role interfaces for the distinct client roles while keeping the class intact.
- **Exceptions/Trade-offs:** Does not mean every two-method interface should be split (PCC-151).
- **Related Rules:** PCC-148, PCC-151.
- **Review Question:** Do different client roles of this class need different subsets of its operations?

#### PCC-151
**Split only when it clarifies dependencies or prevents unsupported behaviour**
- **Source Chapter:** 8 — Applying the Interface Segregation Principle (sections: How ISP relates to LSP and SRP; Summary)
- **Principle:** Segregation has a cost: more abstractions to name, document and maintain. The goal is not the smallest possible interface but a cohesive contract matching its clients' needs.
- **Problem:** Splitting merely to minimise method counts multiplies abstractions without benefit.
- **Detection Signals:**
  - Many tiny interfaces with no distinct client using each separately.
  - Interfaces split although every client uses every member and every implementer supports every member.
- **Recommended Action:** Justify each split by a concrete benefit: a client depending on less, or an implementer no longer forced into unsupported behaviour.
- **Exceptions/Trade-offs:** This rule is the trade-off guidance.
- **Related Rules:** PCC-123, PCC-127, PCC-142, PCC-150.
- **Review Question:** Does this split clarify a client dependency or prevent unsupported behaviour, rather than just shrink the interface?

#### PCC-152
**Before adding a member, check every current and plausible implementer can support it meaningfully**
- **Source Chapter:** 8 — Applying the Interface Segregation Principle (section: Avoiding interface pollution — practical checks table)
- **Principle:** Interface pollution (bloating) grows one member at a time. When adding a method, ask whether every existing and plausible implementation can support it meaningfully.
- **Problem:** A member some implementers cannot support produces stubs (PCC-145) and LSP failures.
- **Detection Signals:**
  - A change that adds an interface member while at least one implementer gets a stub, an empty body or a throw.
- **Recommended Action:** If not every implementer can support it, put the member in a new interface (PCC-144).
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-144, PCC-145, PCC-153.
- **Review Question:** Can every current and plausible implementer support this new member meaningfully?

#### PCC-153
**Before changing an interface, check whether unrelated clients or implementers must rebuild or change**
- **Source Chapter:** 8 — Applying the Interface Segregation Principle (section: Avoiding interface pollution — practical checks table)
- **Principle:** A change to an interface should not force unrelated clients or implementations to rebuild, redeploy or change. If it does, the interface is serving too many unrelated parties.
- **Problem:** Polluted interfaces spread the cost of every change to code that has nothing to do with it; clients must also navigate a large API to reach the few operations they use.
- **Detection Signals:**
  - An interface change touching many files unrelated to the feature.
  - Clients using only a small fraction of the exposed methods (third check of the table).
  - Implementers with members that feel unrelated to the type (second check of the table).
- **Recommended Action:** Segregate the interface so changes affect only the clients and implementers of the capability concerned.
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-143, PCC-148, PCC-152.
- **Review Question:** Would changing this interface force unrelated clients or implementations to change, rebuild or redeploy?

### Code smells & anti-patterns catalogue

| Name | Signal | Rule IDs |
|---|---|---|
| Interface pollution / interface bloating | Many members; clients use a small fraction | PCC-142, PCC-148, PCC-153 |
| Stub implementation | `throw new NotImplementedException()` / empty body for an interface member | PCC-145 |
| Forced capability | Read-only source implementing a write member | PCC-146 |
| Unrelated member added to widely used interface | Interface change benefiting one implementer only | PCC-144, PCC-152 |
| Ripple change | Interface edit forces unrelated clients/implementers to change or rebuild | PCC-153 |
| Broad-typed client | Client depends on a broad interface but calls one member | PCC-148 |
| One-class-per-interface over-split | Classes split only to mirror interfaces, duplicating state | PCC-147 |
| Over-segregation | Many tiny interfaces with no distinct clients | PCC-151 |

### Refactoring techniques named in the chapter

- Split an interface by capability (reader/writer).
- Implement several focused interfaces in one concrete class when it supports them all.
- Retype a client's dependency to the narrowest focused interface it uses (constructor injection of the reader into a printer).
- Introduce a separate interface for a new specialised capability instead of extending the common one (service-boundary analogy).
- Introduce role interfaces for distinct client roles of one cohesive class (push-only view of a stack).
- Practical pollution checks (paraphrase of the chapter's table): adding a member → can every implementer support it?; implementing → do members feel unrelated or need stubs?; using → does the client use only a small fraction?; changing → will unrelated clients/implementers have to change, rebuild or redeploy?

### Things the author says NOT to do mechanically

- Do not treat small size as the goal; an interface is as broad as one coherent capability requires.
- Do not split every two-method interface; segregation costs naming, documentation and maintenance.
- Do not assume every ISP problem is also an SRP problem — it may be, but not automatically.
- Do not assume ISP requires one concrete class per capability.
- Do not add stubs to satisfy an interface — question the interface or the implementation instead.

---

## Chapter 9 — Applying the Dependency Inversion Principle

### Chapter summary

- Dependencies are normal and often the product of good practice (focused, single-responsibility classes). The problem is rigidity: a class bound to one specific low-level class cannot have it swapped safely when requirements change or for testing.
- DIP (two parts): high-level modules should not depend on low-level modules — both depend on abstractions; and abstractions should not depend on details — details depend on abstractions. Simplified: depend on abstractions rather than concrete classes when the behaviour may vary.
- High-level code expresses the application's main behaviour; low-level code handles details such as databases, external APIs and file systems.
- Example: a bookstore bound to one courier class must change (and its tests must change) whenever the courier changes; depending on a delivery abstraction means only the creating code changes. The class depends on what it needs, not on who currently provides it.
- An implementation's own needs (e.g. a file name) belong in that implementation (its constructor), not in the shared abstraction.
- DIP (design principle) is not Dependency Injection (construction technique where a class receives its dependencies). They work well together. When a dependency needs runtime data, inject a factory abstraction.
- Case study: an employee-report design breaks every SOLID principle at once (mixed interface, type-string branching, concrete construction, an override rejecting inputs, a direct static file call). Refactoring in two stages — isolate file access behind a thin wrapper interface, then move report generation to the employee types that own the data — resolves all five. Static `Console` usage remains a DIP issue deferred to chapter 10.

### Rules

#### PCC-154
**Make high-level code depend on abstractions, not on concrete low-level classes**
- **Source Chapter:** 9 — Applying the Dependency Inversion Principle (sections: Understanding the Dependency Inversion Principle; Replacing a concrete delivery service with an abstraction)
- **Principle:** Classes expressing the application's main behaviour should not be tightly coupled to classes handling details (databases, external APIs, file systems). Both should depend on an abstraction. Short form: when behaviour may vary, depend on an appropriate abstraction rather than a concrete implementation class.
- **Problem:** A concrete dependency cannot be swapped easily or safely. Changing the provider breaks compilation where its specific methods were called; if the concrete type is used in dozens of places or dozens of classes, replacing it means edits across all of them, more bug risk, and dozens or hundreds of tests to update.
- **Detection Signals:**
  - Constructor parameters or fields typed as concrete service/provider classes (data access, external API clients, file/IO helpers).
  - Calls to provider-specific method names spread across many classes.
  - Tests that cannot replace the dependency without changing the class under test.
- **Recommended Action:** Introduce an interface describing what the high-level class needs; make the concrete class implement it; type the high-level class's field/constructor parameter by the interface; supply the concrete instance from the creating code.
- **Exceptions/Trade-offs:** The author's qualifier is "when the behavior may vary"; dependencies themselves are normal — the issue is rigidity. The book does not give a list of dependencies that are fine to keep concrete (not stated in book).
- **Related Rules:** PCC-155, PCC-157, PCC-117, PCC-148, ch15 testability.
- **Review Question:** Does this high-level class reference a concrete low-level class whose behaviour might vary or need substituting in tests?

#### PCC-155
**Shape the abstraction around what the client needs, not around who provides it**
- **Source Chapter:** 9 — Applying the Dependency Inversion Principle (section: Replacing a concrete delivery service with an abstraction)
- **Principle:** The abstraction should express the client's need (e.g. "deliver this package to this address"), not mirror the current provider's specific API (e.g. a courier's "deliver small item" method). The client depends on *what* it needs rather than on *who* currently supplies it — like using a delivery broker that finds a suitable courier.
- **Problem:** A provider-shaped interface still couples the client to that provider's vocabulary and limits (small items, domestic only), so a provider change still ripples into the client.
- **Detection Signals:**
  - Interface or method names containing a vendor/provider name or provider-specific constraints.
  - Interfaces extracted mechanically from one concrete class with all its methods.
- **Recommended Action:** Name the interface and its members after the client's purpose; let each provider adapt its own API inside its implementation.
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-154, PCC-156, PCC-142.
- **Review Question:** Would this interface still make sense if a different provider implemented it?

#### PCC-156
**Keep implementation details out of abstractions**
- **Source Chapter:** 9 — Applying the Dependency Inversion Principle (section: Keeping abstractions independent from implementation details)
- **Principle:** An abstraction (interface/abstract class) says *what* is done; a detail (concrete implementation) says *how*. Abstractions must not be shaped by one implementation's incidental needs; details conform to abstractions. Example: if an Excel-based reader needs a file name, that goes to its constructor, not into the shared reader interface also implemented by a cloud-storage reader.
- **Problem:** Putting one implementation's parameter into the interface forces every other implementation to accept a meaningless parameter — a change in a detail altering the abstraction, which breaks DIP.
- **Detection Signals:**
  - Interface method parameters that only one implementation uses (others ignore them).
  - Interface changes triggered by a new requirement of a single implementation.
  - Interface members typed with implementation-specific types (file paths, connection strings) in a contract with non-file/non-DB implementations.
- **Recommended Action:** Move the detail into the implementation's constructor (or its configuration); change the interface only when the abstraction itself must change.
- **Exceptions/Trade-offs:** When the domain itself changes for all implementations, changing the contract is legitimate (PCC-122).
- **Related Rules:** PCC-122, PCC-145, PCC-155.
- **Review Question:** Is every parameter and member of this interface meaningful for every implementation?

#### PCC-157
**Inject dependencies instead of constructing them inside the consumer**
- **Source Chapter:** 9 — Applying the Dependency Inversion Principle (sections: Dependency Inversion and Dependency Injection are not the same; Refactoring a concrete data-provider dependency)
- **Principle:** As a general guideline, a class should receive its dependencies from outside (typically through its constructor) rather than create them. The class then only declares what it needs, not which type it gets or how it is built.
- **Problem:** A class that constructs its dependency must know the dependency's construction details (including what its constructor expects, such as a hard-coded file name) — which is not its responsibility, and real-world construction is often complicated. It cannot be handed a different data source when requirements change, and tests cannot substitute a mock.
- **Detection Signals:**
  - `new ConcreteService(...)` in a constructor or method body of a class that uses it.
  - Hard-coded construction arguments (file names, paths, URLs) inside the consuming method.
  - Unit tests that must touch the real file system/service because the class builds its own collaborator.
- **Recommended Action:** Add a constructor parameter of the abstraction type, store it in a readonly field, use it in place of the `new` expression; move construction to the creating code (PCC-160).
- **Exceptions/Trade-offs:** When the dependency needs data available only at run time, inject a factory instead (PCC-159).
- **Related Rules:** PCC-154, PCC-158, PCC-159, PCC-160, ch10 static, ch15 testability.
- **Review Question:** Does this class create any collaborator it should instead receive?

#### PCC-158
**Don't confuse Dependency Inversion with Dependency Injection; check both**
- **Source Chapter:** 9 — Applying the Dependency Inversion Principle (section: Dependency Inversion and Dependency Injection are not the same — table comparing the two)
- **Principle:** DIP is a design principle: high- and low-level code depend on abstractions, and details conform to them (e.g. the bookstore depends on a delivery interface rather than a specific courier). Dependency Injection is a construction pattern: a class receives the objects it needs instead of building them (e.g. the delivery object passed through the constructor). They complement each other but are not the same thing.
- **Problem:** A field typed as an interface but filled by `new Concrete()` inside the class still forces the class to know how the concrete type is created, still prevents substituting a mock, and still prevents choosing the implementation at run time from configuration or user decisions.
- **Detection Signals:**
  - Interface-typed field assigned with `new SomeConcrete()` in the same class's constructor.
  - Abstractions in signatures but concrete construction inside the class.
  - The reverse case — injection without inversion: a constructor parameter or field typed as a concrete low-level class (`public Store(FastCourier courier)`), so the class still names the detail it depends on. *(added 2026-10-03 after a NotebookLM cross-check of chapter 9)*
- **Recommended Action:** Keep the abstraction (DIP) *and* move construction outside (DI) so implementations can be chosen at run time and replaced in tests.
- **Exceptions/Trade-offs:** Inversion of Control and DI containers: (not stated in book) — the chapter does not discuss IoC as a concept or any container.
- **Related Rules:** PCC-154, PCC-157, PCC-160, PCC-180.
- **Review Question:** Is what this class receives typed as an abstraction, and does the class also avoid constructing the implementation itself?

#### PCC-159
**Inject a factory abstraction when a dependency needs runtime data**
- **Source Chapter:** 9 — Applying the Dependency Inversion Principle (section: Creating dependencies only when runtime data is available)
- **Principle:** If a dependency can only be created once some runtime value is known (the author's example: weather data access requires a country chosen by the user inside the method), inject a factory abstraction whose create method takes that value and returns the dependency's abstraction.
- **Problem:** Constructing the dependency inline (`new X(runtimeValue)`) makes the consumer depend on the concrete type and prevents test substitution; constructor injection of the dependency itself is impossible because the value isn't known yet.
- **Detection Signals:**
  - `new ConcreteType(userInputOrComputedValue)` inside a method of a high-level class.
  - Comments or code indicating "we need X to create this" right before a `new`.
- **Recommended Action:** Define an interface for the dependency and an interface for its factory (`Create(value)`); implement the factory with the single `new`; inject the factory into the consumer's constructor; call it when the value is known. Tests supply a fake factory controlling which dependency is returned.
- **Exceptions/Trade-offs:** The factory still depends on the concrete class — acceptable because that dependency is isolated in one place while the rest of the application works with abstractions.
- **Related Rules:** PCC-121, PCC-157, PCC-160, ch15 testability.
- **Review Question:** When a collaborator needs runtime data, is it created through an injected factory abstraction rather than with `new` in the consumer?

#### PCC-160
**Confine knowledge of concrete types to the creating code, and update it when wiring changes**
- **Source Chapter:** 9 — Applying the Dependency Inversion Principle (sections: Replacing a concrete delivery service with an abstraction; Creating dependencies only when runtime data is available; Isolating file access behind an abstraction)
- **Principle:** With dependencies expressed as abstractions and injected, swapping an implementation affects only the code that creates the objects — a much smaller impact than editing every consumer and its tests. Unavoidable concrete references are isolated in one place (a factory, a wrapper, the creation code).
- **Problem:** When concrete types are known throughout the codebase, a provider change becomes a large, risky, multi-class and multi-test edit.
- **Detection Signals:**
  - Changing a provider requires edits in many consumer classes rather than in one creation point.
  - Refactorings that introduce constructor injection but leave the creating code unadjusted (the author explicitly reminds to update the code that constructs the refactored class).
- **Recommended Action:** Keep `new` of concrete implementations at the creation point; after introducing injection, update that creation code to pass the concrete implementation (e.g. construct the report creator with the file wrapper).
- **Exceptions/Trade-offs:** The creation point/factory is accepted as the place that changes (OCP containment).
- **Related Rules:** PCC-121, PCC-141, PCC-158, PCC-159.
- **Review Question:** If this implementation were replaced, would only the creation code have to change?

#### PCC-161
**Hide static low-level APIs behind a thin, logic-free wrapper interface**
- **Source Chapter:** 9 — Applying the Dependency Inversion Principle (sections: Refactoring a concrete data-provider dependency; Isolating file access behind an abstraction)
- **Principle:** Direct calls to static framework classes (`File.WriteAllText`, `Console`) tie a class to one concrete mechanism and violate DIP. Wrap the static API in a thin class implementing a small interface; the class with business logic depends on the interface.
- **Problem:** Static APIs are hard to substitute in tests or alternative environments; the high-level class cannot change how it communicates or persists without being modified.
- **Detection Signals:**
  - `File.`, `Directory.`, `Console.` (and similar static I/O) calls inside classes that hold business logic.
  - Unit tests that need a real file system or console to run.
- **Recommended Action:** Define an interface with only the needed operation(s); implement a wrapper that forwards to the static API and contains no business logic; inject the interface; tests inject a mock.
- **Exceptions/Trade-offs:** The wrapper itself still calls the static class — acceptable because the unavoidable concrete dependency is isolated in one class with no business logic. The author explicitly leaves the `Console` dependency in the data-provider example unresolved and defers static-method trade-offs to chapter 10.
- **Related Rules:** PCC-154, PCC-162, ch10 static methods and dependencies, ch15 testability.
- **Review Question:** Does any class with business logic call a static I/O API directly instead of an injected abstraction?

#### PCC-162
**Don't expose general low-level utilities on a high-level class's interface**
- **Source Chapter:** 9 — Applying the Dependency Inversion Principle (sections: Identifying the issues; Isolating file access behind an abstraction)
- **Principle:** An interface for a domain operation (creating an employee report) should not also carry an unrelated general operation (saving any text to any file). Separate them: the domain interface keeps its operation; the low-level operation gets its own abstraction used by whoever needs it.
- **Problem:** The mixed interface breaks ISP and pushes implementers toward two responsibilities (SRP); it also invites odd usage — reaching for a report creator just to write a file. Low-level work like file writing should not live in the high-level class.
- **Detection Signals:**
  - Public interface members whose parameters are generic (path, contents) and unrelated to the interface's domain purpose.
  - Domain classes exposing public helper methods for I/O.
- **Recommended Action:** Remove the utility member from the domain interface; create a separate small interface for it (PCC-161); make the former public helper private and delegate to the injected abstraction.
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-142, PCC-146, PCC-161, ch05 SRP.
- **Review Question:** Does this domain interface expose an operation unrelated to its purpose?

#### PCC-163
**Replace type-string branching that picks concrete collaborators with polymorphism**
- **Source Chapter:** 9 — Applying the Dependency Inversion Principle (sections: Identifying the issues; Moving report behavior to the objects that own the data)
- **Principle:** A method that inspects a type field (e.g. comparing a position string to "Developer") and then constructs a different concrete collaborator per branch breaks two principles at once: OCP (each new type needs another branch) and DIP (the method names concrete classes instead of depending on an abstraction, ideally injected).
- **Problem:** Every new kind requires editing the orchestrator, and the orchestrator is coupled to all concrete variants.
- **Detection Signals:**
  - `if (obj.Position == "…")` / string or enum comparisons followed by `new ConcreteA()` / `new ConcreteB()`.
  - A local variable of a base type assigned different concrete instances per branch.
- **Recommended Action:** Move the varying behaviour behind polymorphism — in the case study, into the data-owning types themselves (PCC-165) — so the orchestrator just calls the abstraction. Otherwise depend on an injected abstraction.
- **Exceptions/Trade-offs:** If a selection point is unavoidable, contain it in a factory (PCC-121).
- **Related Rules:** PCC-119, PCC-121, PCC-136, PCC-165.
- **Review Question:** Does this method branch on a type field to decide which concrete class to instantiate?

#### PCC-164
**Avoid overrides that reject base-accepted arguments; treat parallel hierarchies as a warning**
- **Source Chapter:** 9 — Applying the Dependency Inversion Principle (sections: Identifying the issues; Moving report behavior to the objects that own the data)
- **Principle:** Overriding a virtual method forces the subtype to keep the base signature, including a parameter broader than the subtype can handle (a developer-only report generator must still accept any employee). Checking the argument's type and throwing breaks LSP; downcasting the argument to read subtype data is part of the same misuse. Two parallel hierarchies (data types and matching processor types) are the shape of this misuse of inheritance and virtual methods.
- **Problem:** Any code working with the base generator breaks when it receives the specialised generator together with a non-matching argument; the override also duplicates the base logic.
- **Detection Signals:**
  - An `override` whose first statement validates the argument's type/kind and throws.
  - `(param as Subtype).Member` or casts of a base-typed parameter inside an override.
  - Two hierarchies mirroring each other (`X`/`SpecialX` alongside `XProcessor`/`SpecialXProcessor`).
  - Override duplicating the base implementation and appending to it.
- **Recommended Action:** Remove the parallel processor hierarchy and move behaviour onto the data hierarchy (PCC-165); reuse base behaviour via `base.` calls instead of duplicating it.
- **Exceptions/Trade-offs:** The `virtual` keyword itself is not condemned; the author's point is that making the method virtual opened the door to this misuse.
- **Related Rules:** PCC-133, PCC-140, PCC-165.
- **Review Question:** Does any override narrow its accepted input by type-checking or downcasting its parameter?

#### PCC-165
**Move behaviour to the type whose data it uses**
- **Source Chapter:** 9 — Applying the Dependency Inversion Principle (section: Moving report behavior to the objects that own the data)
- **Principle:** A method that mostly uses another type's data probably belongs to that type. In the case study, report generation moves into the employee base type as a virtual method, the developer subtype overrides it and builds on the base result, and the separate generator classes are deleted. The orchestrator simply asks the object for its report.
- **Problem:** Logic living away from its data forces external type checks, concrete construction and subtype-hostile overrides; the result is convoluted.
- **Detection Signals:**
  - A method whose body reads mostly properties of its parameter, not of its own class.
  - Processor classes paired one-to-one with data subtypes.
  - Orchestrators inspecting a type field to pick a processor.
- **Recommended Action:** Move the method into the data type (virtual where variants differ); in subtypes override and reuse the base implementation; delete the processor classes and the selection logic; the caller just invokes the method on the object.
- **Exceptions/Trade-offs:** (not stated in book). After the move, subtypes must remain safely substitutable (the author confirms they are in the case study).
- **Related Rules:** PCC-119, PCC-163, PCC-164, ch13 cohesion (by name).
- **Review Question:** Does this behaviour live in the type that owns the data it works on?

#### PCC-166
**Expect SOLID violations to cluster; audit every principle and refactor in stages**
- **Source Chapter:** 9 — Applying the Dependency Inversion Principle (sections: Refactoring a design that violates several SOLID principles; Understanding the code; Identifying the issues; Improving the code)
- **Principle:** Several SOLID problems tend to feed one another; the right abstractions pull them apart. First understand what the code does, then go through it method by method and map each problem to a principle; fix in stages, starting with the simplest, fastest, most isolated improvement ("low-hanging fruit"), then the deeper restructuring; finally re-check each principle.
- **Problem:** Small, working code can still be convoluted in ways that make every change harder; fixing one symptom in isolation can miss related violations.
- **Detection Signals (from the case study's issue table):**
  - Interface mixing domain operation and file writing → ISP/SRP.
  - If/else chain selecting generator types → OCP.
  - Orchestrator constructing concrete generators → DIP.
  - Subtype rejecting base-valid input → LSP.
  - Direct static file call and self-created dependencies → DIP/testability.
- **Recommended Action:** Build a problem → principle → reason table; stage 1: isolate low-level access behind an abstraction; stage 2: restructure behaviour (move to data owners, remove branching and parallel hierarchies); then walk SRP, OCP, LSP, ISP, DIP again to confirm each is resolved.
- **Exceptions/Trade-offs:** (not stated in book).
- **Related Rules:** PCC-161, PCC-162, PCC-163, PCC-164, PCC-165, PCC-127, PCC-149.
- **Review Question:** Has each finding been mapped to the principle it breaks, and has the refactored design been re-checked against all five?

### Code smells & anti-patterns catalogue

| Name | Signal | Rule IDs |
|---|---|---|
| Concrete low-level dependency | Field/constructor parameter typed as a concrete service/provider class | PCC-154 |
| Provider-shaped abstraction | Interface/method named after a vendor or its specific constraints | PCC-155 |
| Detail leaking into abstraction | Interface parameter meaningful for only one implementation | PCC-156 |
| Self-constructed dependency | `new Service(...)` inside the consumer's constructor or method; hard-coded file names | PCC-157 |
| Interface field, concrete construction | Interface-typed field assigned `new Concrete()` in the same class | PCC-158 |
| Inline runtime construction | `new X(runtimeValue)` inside a high-level method | PCC-159 |
| Concrete knowledge spread across consumers | Provider change requires edits in many classes and tests | PCC-160 |
| Hidden static dependency | `File.` / `Console.` calls inside business classes | PCC-161 |
| Utility member on domain interface | Public save-text-to-file style member on a domain interface | PCC-162 |
| Type-string dispatch with construction | `if (x.Position == "...") gen = new A(); else gen = new B();` | PCC-163 |
| Type-guarding override | Override validates argument kind and throws | PCC-164, PCC-140 |
| Downcast inside override | `(param as Subtype).Member` | PCC-164 |
| Parallel inheritance hierarchies | Data hierarchy mirrored one-to-one by processor hierarchy | PCC-164, PCC-165 |
| Behaviour away from its data | Method reads mostly another type's properties | PCC-165 |
| Clustered SOLID violations | Small working code breaking several principles at once | PCC-166 |

### Refactoring techniques named in the chapter

- Introduce an abstraction (interface) for a concrete low-level dependency and depend on it.
- Constructor injection: receive the dependency through the constructor, store it in a readonly field.
- Move implementation-specific configuration (file name) into the implementation's constructor.
- Factory abstraction for runtime-data dependencies (`IXFactory.Create(value)` returning the dependency's interface; the concrete `new` lives only in the factory).
- Thin wrapper class around a static framework API (file writing) implementing a small interface, with no business logic.
- Split a mixed interface (domain operation vs. file writing) into separate interfaces; make the leftover helper private and delegate to the injected abstraction.
- Move a method to the type whose data it uses; make it virtual and override in subtypes reusing the base implementation; delete the parallel generator hierarchy and the type-branching.
- Staged refactoring: low-hanging fruit first (isolate file access), then restructure behaviour; re-check every SOLID principle at the end.
- Update the creation code after introducing injection.

### Things the author says NOT to do mechanically

- Do not treat dependencies as bad in themselves — the problem is rigid coupling to one concrete class where behaviour may vary.
- Do not change an interface because one implementation has a new requirement; change it only when the abstraction itself changes.
- Do not equate Dependency Inversion with Dependency Injection — an interface-typed field built with `new` inside the class is not decoupled.
- Do not insist on constructor injection when the dependency needs runtime data — use an injected factory.
- Do not try to remove every concrete reference: factories and thin wrappers may keep one, provided it is isolated and carries no business logic.
- Do not assume the static `Console` usage is resolved by this chapter — the author explicitly defers it to chapter 10.

# Cross-chapter relations (as stated in these chapters)

| Relation | What the book says | Rule IDs |
|---|---|---|
| SRP ↔ OCP | Different questions (one coherent responsibility vs. adding expected variation without editing stable code); moving variants into focused classes satisfies both; neither may be applied mechanically. | PCC-127 |
| LSP → OCP | Client-side subtype checks break LSP *and* create an OCP problem (one more branch per exceptional subtype). | PCC-136 |
| LSP → ISP | Splitting a fuel capability into a narrower and an extended interface "foreshadows" ISP. | PCC-138 |
| ISP → LSP | An oversized interface forcing a throwing stub is an ISP problem that creates the conditions for an LSP failure. | PCC-145, PCC-149 |
| ISP ↔ SRP | ISP problems may contribute to SRP problems but not automatically; SRP and ISP can point in different directions (stack example). | PCC-149, PCC-150 |
| OCP containment ↔ LSP/DIP factories | Isolating the creation/selection decision in one factory is reused in the LSP configuration refactoring and the DIP runtime-data factory. | PCC-121, PCC-141, PCC-159, PCC-160 |
| DIP vs DI | DIP = design principle (depend on abstractions; details conform); DI = construction technique (receive dependencies instead of creating them); complementary but distinct. | PCC-158 |
| DIP vs IoC | (not stated in book) — Inversion of Control and DI containers are not discussed in these chapters. | — |
| OCP vs YAGNI | The chapter warns against abstractions for every imaginable future requirement and recommends modelling only current or reasonably likely variation; the term YAGNI itself appears in chapter 13, not in chapter 6. | PCC-123 |
| DIP ↔ static methods | Direct use of static `Console`/`File` breaks DIP; wrapper shown for `File`; `Console` deferred to chapter 10. | PCC-161 |
| DIP ↔ testability | Injection lets tests pass mocks of the abstraction or of a factory. | PCC-157, PCC-159, PCC-161 |

---

## Chapter 10 — Static methods and dependencies

### Chapter summary
- A static member belongs to the type, not to any object. A static method can therefore see only its parameters and static fields. Static fields and properties are state shared by the whole application.
- Static is sometimes correct and sometimes more expensive than it looks. The chapter's purpose is to tell the two cases apart.
- Core reasoning: a static call is a dependency that never appears in a constructor or a signature. Because nothing declares it, nothing can replace it, whether for a behaviour change or for a test.
- Two questions decide whether a method should be static: does it use instance state, and might a caller ever need to replace or control its behaviour? Private stateless helpers usually pass. Public replaceable behaviour does not.
- Making a public method static throws away what the Dependency Inversion Principle provided: no alternative implementation, no mock, and no interface implementation at all. Interface members are implicitly virtual, and a member cannot be both virtual and static.
- Public static is appropriate only for behaviour that will never have another implementation and never needs mocking, such as maths or simple text transforms. Data access, business rules and UI interaction do not qualify.
- Static framework APIs you cannot change (`DateTime.Now`, `File`, `Console`) should be wrapped behind your own interface and injected when behaviour must be controlled. `TimeProvider` (.NET 8+) is the built-in option for time.
- Public static methods and public static fields let any code acquire a dependency silently. This "obscures the dependency graph": objects need secret `Initialize` calls, setup order matters, and tests are slow. The cure is to put every required collaborator in the constructor as an abstraction, so a missing dependency becomes a compile error and tests can pass doubles.

### Rules

#### PCC-167
**Decide static-ness on state use *and* replaceability**
- **Source Chapter:** 10 — Static methods and dependencies (section: Deciding when a method should be static; Static methods at a glance)
- **Principle:** A method that touches no instance state *can* be static. Whether it *should* be also depends on whether any caller might need to swap or control its behaviour.
- **Problem:** If "uses no fields" is treated as enough, behaviour that holds no state but is replaceable (for example, a database reader) gets hard-wired into every caller.
- **Detection Signals:**
  - A diff adds `static` to an existing **public** method and rewrites call sites from `_field.Method()` to `TypeName.Method()`.
  - Analyzer/IDE "member can be marked static" suggestions (e.g. CA1822) accepted wholesale on public members **(applied)**.
  - `static` methods whose bodies do I/O, read configuration, or contain business rules.
- **Recommended Action:** Ask both questions. Make a method static only when it uses no instance state **and** nobody will ever need a different implementation or a test double.
- **Exceptions/Trade-offs:** For private methods the replaceability question rarely applies (see PCC-168).
- **Related Rules:** PCC-168, PCC-169, PCC-171; ch9 DIP.
- **Review Question:** For each method made static, has someone confirmed that no caller will ever need to swap or mock it?

#### PCC-168
**Make stateless private helpers static**
- **Source Chapter:** 10 — Static methods and dependencies (section: Private methods)
- **Principle:** A private method that uses no instance data is usually better marked `static`. The modifier tells the reader from the signature that object state is irrelevant.
- **Problem:** Without the modifier, a reader has to scan the class's fields to find out whether the helper reads or changes them.
- **Detection Signals:**
  - Private instance methods that reference no field, property or other instance member.
  - Parsing/conversion helpers like the book's ch11 example that builds one ingredient from one text line.
- **Recommended Action:** Add `static` and pass everything the method needs as parameters.
- **Exceptions/Trade-offs:** The book says the answer is "often" yes, not always. Being static does not settle *where* a helper belongs: a low-level mechanism inside a high-level class should still move out (PCC-195).
- **Related Rules:** PCC-167, PCC-195, PCC-200.
- **Review Question:** Does every private method that ignores instance state declare that with `static`?

#### PCC-169
**Don't make public, replaceable behaviour static**
- **Source Chapter:** 10 — Static methods and dependencies (section: Why public static methods need more caution)
- **Principle:** A public method that represents behaviour someone might swap (a data source, a business rule) stays an instance member behind an abstraction, even if it holds no state.
- **Problem:** In the book's example, `DataAccess.GetData` becomes static. The consumer then calls the type name, and the injected interface and object disappear. There is no way to switch to a file or API source and no way to mock it in a unit test. The consumer is bound to one concrete class.
- **Detection Signals:**
  - Business methods call project-defined `SomeType.Method()` where `SomeType` does I/O or domain logic.
  - A consumer constructor that used to take a collaborator now takes nothing.
  - `public static` methods named `Get*`, `Load*`, `Read*`, `Save*`, `Calculate*` on project classes. `static class *Repository`, `*DataAccess`, `*Service`.
- **Recommended Action:** Keep the method on an instance. Extract an interface (`IDataAccess`), have the class implement it, store it in a `private readonly` field, and inject it through the consumer's constructor.
- **Exceptions/Trade-offs:** Pure, stable operations (PCC-171).
- **Related Rules:** PCC-170, PCC-172, PCC-179, PCC-180; ch9 DIP; ch15 testability.
- **Review Question:** Could this public static method ever need a second implementation or a mock? If yes, why is it static?

#### PCC-170
**Keep polymorphism available: static members cannot implement interfaces or be overridden**
- **Source Chapter:** 10 — Static methods and dependencies (section: Why public static methods need more caution; Figure 10.1)
- **Principle:** Interface implementations are implicitly virtual. Virtual dispatch picks the implementation from the object's runtime type, and a static call has no object. So a member is either virtual or static, never both. Making it static means the class can no longer implement the interface or be used polymorphically.
- **Problem:** The type can no longer be substituted where an abstraction is expected. The code becomes rigid.
- **Detection Signals:**
  - A class that has, or clearly should have, an interface, but whose operations are `static`.
  - Compile errors after making an interface-implementing method static.
  - A `static class` whose operations mirror an interface that consumers would like to depend on.
- **Recommended Action:** Keep these members instance-level, implement the interface, and make consumers depend on the interface.
- **Exceptions/Trade-offs:** (not stated in book) beyond the PCC-171 cases.
- **Related Rules:** PCC-169, PCC-180; ch7 LSP; ch9 DIP.
- **Review Question:** Does making this member static stop its type from implementing an interface or being substituted?

#### PCC-171
**Reserve public static for pure, stable operations that never need substitutes**
- **Source Chapter:** 10 — Static methods and dependencies (section: When public static methods make sense)
- **Principle:** A public method may be static when the same implementation is always wanted and it will never be replaced, including by a mock. Examples: `Math.Min` and basic arithmetic (maths doesn't change), and simple text transforms such as stripping whitespace.
- **Problem:** (inverse rule) Such operations have no meaningful alternative. They are fast and deterministic, so a test *wants* the real implementation. Faking them gains nothing.
- **Detection Signals (to accept static):**
  - Output depends only on the inputs, with no I/O, no clock, no randomness and no static mutable state.
  - Deterministic value transforms (the book's `StringExtensions.RemoveExcessiveSpaces` in ch11). Unit conversion or vector maths **(applied)**.
- **Recommended Action:** A static method or extension method is fine. Do not wrap it.
- **Exceptions/Trade-offs:** The author warns this test rules out most methods: anything implementing a business requirement may need another implementation one day. If there is *any* chance it will be mocked, it must not be static.
- **Related Rules:** PCC-167, PCC-172, PCC-195.
- **Review Question:** Is this public static method a deterministic transform with no plausible alternative implementation and no reason to fake it in a test?

#### PCC-172
**Keep data access, business rules and UI interaction out of public static methods**
- **Source Chapter:** 10 — Static methods and dependencies (section: When public static methods make sense)
- **Principle:** Methods that provide data, implement business requirements or talk to the user are expected to change implementation (database → file → cloud storage; console → graphical UI). They must also be mockable, because unit tests should not hit real data sources or real UI.
- **Problem:** Tests lose control of what they exercise, and changing the data source or UI means editing every consumer.
- **Detection Signals:**
  - `public static` methods that open connections, read files, call web APIs, or write to the console or show dialogs.
  - Static helpers that wrap UI prompts such as `TaskDialog.Show` / `MessageBox.Show` and are called from business logic **(applied)**.
- **Recommended Action:** Make it an instance class with an interface and inject it through the constructor.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-169, PCC-173, PCC-181; ch15 testability.
- **Review Question:** Does any public static method here read data or interact with the user?

#### PCC-173
**Wrap static framework APIs you need to control behind an injected interface**
- **Source Chapter:** 10 — Static methods and dependencies (section: Wrapping static framework APIs)
- **Principle:** When a static you depend on comes from the framework or a package (`DateTime.Now`, `File`, `Console`), define your own interface plus a thin wrapper that forwards to the static. Inject the interface.
- **Problem:** A method that formats "today" returns different results on different days, so a unit test cannot pin it. A static property cannot be replaced through interface-based mocking, and the clock dependency sits hidden in a method body.
- **Detection Signals:**
  - `DateTime.Now` (and similar clock reads **(applied)**: `DateTime.Today`, `DateTime.UtcNow`) inside logic.
  - Direct `File.*` / `Console.*` calls in classes that have unit tests.
  - Tests that are flaky by date, or that can only assert loosely.
- **Recommended Action:** Create an interface that exposes the value (e.g. `Now`) and a wrapper class that returns the real static value. Take the interface in the consumer's constructor. Production passes the wrapper, tests pass a fake that returns a fixed value. The dependency is now visible in the constructor.
- **Exceptions/Trade-offs:** Wrap only when behaviour must be controlled, typically for tests. Stable pure statics such as `Math` need no wrapper (PCC-171). For time on .NET 8+, see PCC-174.
- **Related Rules:** PCC-174, PCC-175, PCC-180, PCC-181; ch9 DIP; ch15 testability.
- **Review Question:** Does logic under test read the clock, file system or console directly instead of through an injected abstraction?

#### PCC-174
**Prefer `TimeProvider` for current time in new .NET 8+ code, and keep the wrapping skill**
- **Source Chapter:** 10 — Static methods and dependencies (section: Wrapping static framework APIs — note)
- **Principle:** `TimeProvider`, added in .NET 8, is a built-in testable replacement for `DateTime.Now` in newer projects. `DateTime.Now` will stay common in existing code, and most other statics (`File`, `Console`, …) have no framework alternative, so hand-written wrappers are still a needed skill.
- **Problem:** Hand-rolling a clock wrapper where the framework already provides one adds code for no benefit. Assuming every static has a built-in alternative leaves others unwrapped.
- **Detection Signals:**
  - New code targeting .NET 8+ that uses `DateTime.Now` or adds yet another custom clock interface.
  - Several clock abstractions co-existing **(applied)**.
- **Recommended Action:** Inject `TimeProvider` in new .NET 8+ code. Keep or introduce wrappers for other statics.
- **Exceptions/Trade-offs:** The book frames `TimeProvider` as a choice for newer projects. Older code bases and older targets (e.g. a `net48` target in a multi-targeted add-in **(applied)**) keep the wrapper approach.
- **Related Rules:** PCC-173.
- **Review Question:** In .NET 8+ code, is time obtained through an injected `TimeProvider` (or an existing wrapper) rather than `DateTime.Now`?

#### PCC-175
**Keep the dependency graph visible on the class surface**
- **Source Chapter:** 10 — Static methods and dependencies (section: Keeping the dependency graph visible)
- **Principle:** You should be able to see every type a class depends on from its constructor and signatures, without reading method bodies. Hidden collaborators "obscure the dependency graph" and make the class misrepresent itself.
- **Problem:** In the `BankAccount` example, the class looks ready to use. Its first test fails with an obscure error, and only by asking senior colleagues does a newcomer learn that two services must be initialised first. The knowledge lives in people's heads, not in the code.
- **Detection Signals:**
  - A parameterless constructor on a class whose method bodies call `SomeType.StaticMethod()` or `SomeType.SomeStaticField.Method()`.
  - Singletons or static service holders (`X.Instance`) referenced inside methods **(applied)**.
  - First use throws "not initialised" style exceptions.
  - A test needs setup lines unrelated to the class under test.
- **Recommended Action:** Turn each hidden collaborator into a constructor parameter typed as an abstraction (PCC-180), and remove the static access.
- **Exceptions/Trade-offs:** The author qualifies this: code is clearer with explicit dependencies "in many cases", so it is not absolute. Pure static utilities (PCC-171) are not hidden *replaceable* dependencies.
- **Related Rules:** PCC-176, PCC-178, PCC-179, PCC-180; ch9 DIP; PCC-203.
- **Review Question:** Can you list every collaborator this class needs by reading only its constructor?

#### PCC-176
**Objects must be ready to use as soon as they are constructed**
- **Source Chapter:** 10 — Static methods and dependencies (section: Why hidden dependencies create fragile code; Making dependencies explicit)
- **Principle:** No secret initialisation steps or special calls may be needed before an object works. The only setup belongs in its constructor, and whatever the type needs it requests there. Initialisation methods that must run first are hidden prerequisites. Construct objects in a valid state.
- **Problem:** Needing "magic setup" is a sign of flawed design. Users discover the requirement through runtime failures, and the real services make tests slow (20 seconds in the example).
- **Detection Signals:**
  - Methods named `Initialize`/`Init`/`Setup`/`Configure` (static or instance) that must run before use.
  - Comments of the form "call X before Y".
  - Guards throwing `InvalidOperationException` when not initialised.
  - Two-phase construction (`new X(); x.Init();`).
  - Test fixtures calling global initialisers.
- **Recommended Action:** Move required setup into the constructor, require collaborators as constructor parameters, and delete the static `Initialize` methods.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-175, PCC-177, PCC-180, PCC-181.
- **Review Question:** Can an instance be used correctly straight after `new`, with no other call first?

#### PCC-177
**Don't let correctness depend on a hidden initialisation order**
- **Source Chapter:** 10 — Static methods and dependencies (section: Why hidden dependencies create fragile code)
- **Principle:** If global initialisers must run in a particular order, the real dependency graph is more complex than the code admits. Express the ordering through constructor dependencies instead.
- **Problem:** A refactoring that swaps two initialisation lines makes tests fail, and diagnosing the cause takes time.
- **Detection Signals:**
  - Sequences like `A.Initialize(); B.Initialize();` in startup code or test setup.
  - Comments such as "must be called after".
  - Breakage after reordering startup lines.
- **Recommended Action:** Each service requires its prerequisites in its own constructor (the book's transfer manager takes the internal queue). Construction then enforces the order, and the compiler catches omissions.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-176, PCC-180.
- **Review Question:** If two initialisation lines were swapped, would anything break without a compile error?

#### PCC-178
**Avoid public static fields that expose services or shared state**
- **Source Chapter:** 10 — Static methods and dependencies (section: Why hidden dependencies create fragile code)
- **Principle:** Public static fields create global state, meaning data reachable from anywhere. Any code can come to depend on it without declaring that. It is the static-method problem applied to data instead of behaviour.
- **Problem:** A class with an empty constructor can still depend on a service fetched through another type's public static field. That dependency appears in neither its constructor nor its interface.
- **Detection Signals:**
  - `public static I…Service Something;`, `public static … Instance`, or static settable properties holding services.
  - Call chains like `OtherType.SomeService.Method()` in method bodies.
  - Static mutable collections or settings read by many classes.
- **Recommended Action:** Remove the public static holder and pass the instance to dependents through their constructors.
- **Exceptions/Trade-offs:** The warning targets **public** static members. Private, immutable statics (e.g. the `private static readonly Regex` inside the ch11 extension class) appear in the book without criticism. Do not flag them mechanically. A private static field holding a window instance **(applied)** is not the public global state the chapter describes, though it is still state worth reasoning about.
- **Related Rules:** PCC-175, PCC-179.
- **Review Question:** Is any service instance or mutable state reachable through a public static field or property?

#### PCC-179
**Prefer instance members so a missing dependency is a compile error**
- **Source Chapter:** 10 — Static methods and dependencies (section: Why hidden dependencies create fragile code; Making dependencies explicit)
- **Principle:** Any code anywhere can call a public static method, which creates a hidden dependency. An instance method needs an instance, and that instance has to be supplied (constructor or parameter). The code will not compile unless the dependency is provided.
- **Problem:** A forgotten dependency surfaces as a runtime failure or a mysterious test failure instead of a build error, and using the class requires secret knowledge.
- **Detection Signals:**
  - Service-like project types consumed via `Type.StaticMethod()`.
  - Defects found only at runtime because a static service was never set up.
  - `NullReferenceException` from an unset static **(applied)**.
- **Recommended Action:** Convert the service to an instance class behind an interface and inject it, so the compiler enforces its presence.
- **Exceptions/Trade-offs:** Pure, stable operations (PCC-171).
- **Related Rules:** PCC-169, PCC-175, PCC-180.
- **Review Question:** If someone forgot to provide this collaborator, would the build fail?

#### PCC-180
**Require collaborators through the constructor, typed as abstractions, at every level**
- **Source Chapter:** 10 — Static methods and dependencies (section: Making dependencies explicit; Why public static methods need more caution)
- **Principle:** If A needs B, A's constructor takes B. If B needs C, B's constructor takes C. Type each as an interface, so implementations can vary and the consumer does not know which one it received.
- **Problem:** A concrete constructor dependency still violates DIP. Changing the data source means editing the consumer, at least its constructor, and no mock can be substituted.
- **Detection Signals:**
  - Constructors taking concrete infrastructure classes (`DataAccess` rather than `IDataAccess`).
  - Dependency fields not assigned in the constructor or not `readonly`.
  - `new ConcreteService()` inside methods **(applied; see ch9)**.
- **Recommended Action:** Define the interface, store it in a `private readonly` field assigned in the constructor, and repeat down the chain.
- **Exceptions/Trade-offs:** Whether a DI container is needed is not discussed here; the book's tests construct objects by hand. ch9 covers DIP versus DI.
- **Related Rules:** PCC-169, PCC-175–PCC-179, PCC-181, PCC-200; ch9 DIP.
- **Review Question:** Are all required collaborators constructor parameters typed as abstractions?

#### PCC-181
**Use the constructor seam to keep unit tests off real infrastructure**
- **Source Chapter:** 10 — Static methods and dependencies (section: Making dependencies explicit)
- **Principle:** Constructor injection creates a natural testing seam: a unit test passes controlled test doubles instead of starting real infrastructure. Once real dependencies run, the dependencies are being tested too, and it is no longer a unit test.
- **Problem:** Real dependencies make tests slow (the 20-second service start-up), non-isolated, and sometimes expensive (test suites that call real cloud services add to the bill on every run).
- **Detection Signals:**
  - Unit tests calling global `Initialize()` methods.
  - Unit tests opening database, network or cloud connections.
  - Unit tests that take seconds.
  - Tests that depend on setup order.
- **Recommended Action:** Pass mocks or fakes through the constructor (the book uses `Mock<IInterface>().Object`).
- **Exceptions/Trade-offs:** The book says unit tests should *generally* avoid real external dependencies. Tests that deliberately use them are a different category. Collaborators that cannot be abstracted are (not stated in book).
- **Related Rules:** PCC-173, PCC-176, PCC-180; ch15 testable code.
- **Review Question:** Can this class's unit tests run in milliseconds using only test doubles?

### Code smells & anti-patterns catalogue

| Name | Signal | Rule IDs |
|---|---|---|
| Static replaceable behaviour | `public static` Get/Load/Save/Calculate on a project class doing I/O or business logic | PCC-169, PCC-172 |
| Lost polymorphism | Members made static where an interface/virtual member is needed; compile errors implementing an interface | PCC-170 |
| Hidden static dependency | `TypeName.Method()` / `TypeName.Field.Method()` inside a body; constructor declares nothing | PCC-175, PCC-179 |
| Obscured dependency graph | Collaborators discoverable only by reading method bodies | PCC-175 |
| Secret / magic initialisation | `Initialize()` required before use; two-phase construction | PCC-176 |
| Order-dependent initialisation | `A.Initialize(); B.Initialize();` order matters | PCC-177 |
| Global state via public static field | `public static IService X;` or static mutable settings | PCC-178 |
| Uncontrolled framework static | `DateTime.Now`, `File.*`, `Console.*` in logic under test | PCC-173, PCC-174 |
| Concrete dependency (DIP violation) | Constructor parameter of a concrete infrastructure class | PCC-180 |
| Slow / costly "unit" tests | Real DB, cloud or service start-up in unit tests; seconds per test | PCC-181 |
| Unmarked stateless private helper | Private method uses no instance member but isn't `static` | PCC-168 |

### Refactoring techniques named in the chapter
- Mark private, stateless helpers `static`.
- Extract an interface from a concrete dependency and inject it through the constructor (DIP).
- Revert a public static method to an instance method so it can implement an interface again.
- Wrap a static framework API in your own interface plus a forwarding wrapper class (`IDateTime` / `DateTimeWrapper`), and inject it.
- Adopt `TimeProvider` (.NET 8+) for time.
- Replace static `Initialize` calls and public static service fields with constructor-required dependencies, chained down the graph.
- Pass test doubles (Moq mocks) through the constructor in unit tests.

### Things the author says NOT to do mechanically
- Don't make a method static just because it touches no instance state. Public methods also need the "never replaced, never mocked" test.
- Don't treat all statics as bad. Private stateless helpers, maths and simple text processing are legitimately static.
- Don't wrap pure, stable statics like `Math`. Wrap framework statics only when their behaviour must be controlled.
- The explicit-dependency rule is framed as clearer "in many cases", so apply judgement, not a ban on every `static` keyword.
- `TimeProvider` is for newer projects. `DateTime.Now` in existing code is expected and needs wrapping only where control matters.
- Private immutable statics (e.g. a cached `Regex`) are not the global state the chapter warns about.

---

## Chapter 11 — Designing smaller classes

### Chapter summary
- Classes drift. A class starts as a home for a little related behaviour and becomes the place every requirement lands, collecting unrelated data, dependencies, rules, utilities and special cases. Eventually small changes require understanding irrelevant code. Small classes fit in one's head and can be read, changed and tested in isolation.
- The author treats size as arguably the most influential class-quality property. The god class (does too many things, touched by every change, thousands of lines) is called one of the most dangerous anti-patterns: unreadable, untestable, risky to change, painful to debug and hard to reuse.
- The Single Responsibility Principle is the main tool. But a large class is a warning sign, not an automatic failure. Cohesive large classes such as `List<T>` (more than 1,200 lines) are fine, and splitting them to meet a line limit does more harm than the length does.
- Superficial shortening hurts clarity. Examples are `<Original>Helper` classes that cut one responsibility in half and "clever hacks" that compress simple code. Good refactoring often makes the code base longer.
- Class boundaries can be found four ways. **Names**: compound "And" names, incomplete names, vague catch-all names, names mixing abstraction levels, names that change with every requirement. **Data/dependency clusters**. **Change history**. **Abstraction levels**.
- Extract only coherent responsibilities. Avoid both the all-owning class and swarms of meaningless tiny types. The author gives six questions: same data? same change reasons? same abstraction level? honest name? better encapsulation? useful on its own?
- Case study: `PizzaGenerator` is split into `IngredientsRepository`, `Ingredient.IsOkInDiet` and `CollectionRandomizer`. Each extracted service is injected as an abstraction, leaving the generator with one responsibility.

### Rules

#### PCC-182
**Keep data and helper methods private; expose only the intended surface**
- **Source Chapter:** 11 — Designing smaller classes (section: Reviewing class-design fundamentals → Public and private members; Fields and properties)
- **Principle:** Public members are the surface other objects use, and private members are implementation details. Keep data and helpers private. Use properties when the class must control what happens on read/write, or must expose a value that can be read but not written.
- **Problem:** Unrelated code starts touching details it has no business knowing.
- **Detection Signals:**
  - Public fields.
  - Public setters on values only the class itself should change (compare ch10's `Balance { get; private set; }`).
  - Helper methods made public only so one other class can call them.
- **Recommended Action:** Reduce visibility. Use get-only or private-set properties.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-197; ch13 coupling.
- **Review Question:** Is every public member something callers genuinely need?

#### PCC-183
**Recognise and dismantle god classes**
- **Source Chapter:** 11 — Designing smaller classes (section: Understanding the dangers of large classes. God class antipattern)
- **Principle:** A class that does too many things must be broken up by responsibility, using SRP as the main tool. Typical shape: multiple responsibilities, dozens or hundreds of members, thousands of lines, involved in everything.
- **Problem:**
  - Nobody reads it whole, and nobody can tell detail from substance.
  - Changes ripple into distant code.
  - Tests are poor or missing, so every change is a gamble.
  - Finding a bug among thousands of lines is slow.
  - Reusing one method drags along an object that needs dozens of constructor parameters and complex setup.
- **Detection Signals:**
  - Every feature change, whatever its topic, touches this file (`git log` frequency).
  - Thousands of lines; dozens of fields/properties; a long constructor parameter list.
  - Little or no test coverage.
  - Methods with no relation to one another.
- **Recommended Action:** Identify the responsibilities (PCC-188–PCC-195) and extract each into a focused, named class.
- **Exceptions/Trade-offs:** Large but cohesive classes (PCC-185).
- **Related Rules:** PCC-184, PCC-193–PCC-196; ch5 SRP; ch15 testability.
- **Review Question:** Does nearly every change, whatever it concerns, have to pass through this class?

#### PCC-184
**Treat class size as a prompt to investigate, not a verdict**
- **Source Chapter:** 11 — Designing smaller classes (section: Recognizing when a large class is acceptable; Avoiding helper classes that only hide size)
- **Principle:** A large class is a good reason to stop and look for something to extract. The deciding question is whether every member serves one purpose, not the line count. A class over about 200 lines is not automatically a failure.
- **Problem:** Splits driven by a line limit can do more damage than the length ever would.
- **Detection Signals:**
  - Review comments like "too long, split it" that don't name a second responsibility.
  - Splits justified only by a size threshold, including repository guidelines such as "C# file < 300 lines" or "consider modularising > 200 lines" **(applied)**.
- **Recommended Action:** List what the class does. If an honest look, ideally with a colleague or two, finds one responsibility, leave it.
- **Exceptions/Trade-offs:** The author stresses that large classes are justified *rarely*; most are not fine. This rule protects against forced shortening, not against splitting real god classes.
- **Related Rules:** PCC-183, PCC-185, PCC-186.
- **Review Question:** Can you name a second responsibility in this large class? If not, what does splitting it buy?

#### PCC-185
**Keep a large class intact when its size comes from one cohesive purpose**
- **Source Chapter:** 11 — Designing smaller classes (section: Recognizing when a large class is acceptable)
- **Principle:** Data structures can be large and still follow SRP. `List<T>` has many operations, all serving "a growable collection of one type". Splitting it (one class to add, another to remove) would be forced.
- **Problem:** A forced split breaks encapsulation. Extracted parts would need access to the private data, and if they can reach inside, so can anything else.
- **Detection Signals:**
  - A large class where all members operate on the same private state.
  - Proposed split classes named by verbs over the same data (`XAdder` / `XRemover`).
  - An extraction that would require exposing former private fields.
- **Recommended Action:** Leave it. Size that comes from the number of operations a concept must support is legitimate.
- **Exceptions/Trade-offs:** Size from a *mix of unrelated jobs* is not covered; that is a god class (PCC-183).
- **Related Rules:** PCC-184, PCC-197.
- **Review Question:** Do all these members serve one responsibility over shared private state?

#### PCC-186
**Don't extract helper classes merely to hide size**
- **Source Chapter:** 11 — Designing smaller classes (section: Avoiding helper classes that only hide size)
- **Principle:** Extracting a class is usually good when it represents a concept of its own. Moving methods out only to reduce the method count, when they still belong with those left behind, splits one responsibility in half rather than separating two.
- **Problem:** Understanding one thing now takes two files.
- **Detection Signals:**
  - New classes named `<Original>Helper` (the book's tell: `Abc` → `AbcHelper`), `<Original>Utils` or `<Original>Part2` **(applied)**.
  - An extracted class used only by its origin, with data shuttled back and forth and mutual calls between the pair.
- **Recommended Action:** Merge it back, or find the real concept inside and give it an honest, specific name.
- **Exceptions/Trade-offs:** A helper that represents its own concept is a good extraction.
- **Related Rules:** PCC-184, PCC-190, PCC-196.
- **Review Question:** Could the extracted class be given an honest name other than "<Original>Helper"?

#### PCC-187
**Never trade clarity for brevity**
- **Source Chapter:** 11 — Designing smaller classes (section: Avoiding superficial ways of making code shorter)
- **Principle:** Clarity comes first. Shorter is better only when it is not harder to follow. Judge by how easily the intended readers understand the code, not by physical lines. Good refactoring (new classes, extracted methods, descriptive names) often makes the code base longer, and that is fine.
- **Problem:** "Clever hacks" compress simple code into denser forms that take far longer to understand. The book's example rewrites a plain loop that sums even numbers as a single range/filter/sum expression.
- **Detection Signals:**
  - Dense one-liners replacing a simple loop.
  - Chained expressions relying on arithmetic tricks.
  - PR descriptions justifying a change by "fewer lines".
  - Short cryptic names introduced to save space.
- **Recommended Action:** Pick the form the team reads most easily, and let code review decide rather than a line-count contest.
- **Exceptions/Trade-offs:** Teams legitimately prefer different idioms. This is not a ban on LINQ, but readability for the intended readers decides.
- **Related Rules:** PCC-184, PCC-196; ch2 Meaningful names; ch3 Writing better methods.
- **Review Question:** Is the shorter version actually quicker for this team to understand?

#### PCC-188
**Split a class whose honest name needs "And"**
- **Source Chapter:** 11 — Designing smaller classes (section: Using class names to identify responsibilities → Compound names reveal multiple jobs)
- **Principle:** A focused class is easy to name. A literal name joined with "And" (the appointments confirmer-and-notification-sender) exposes two responsibilities.
- **Problem:** The two jobs cannot be named honestly or changed independently.
- **Detection Signals:**
  - Class names containing `And` (or `Or` **(applied)**).
  - Very long compound names.
- **Recommended Action:** Extract each responsibility into its own class (`AppointmentsConfirmer`, `PatientNotificationSender`).
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-189, PCC-190; ch2 Meaningful names; ch5 SRP.
- **Review Question:** Can the class's full behaviour be described without the word "and"?

#### PCC-189
**Don't hide extra responsibilities behind a narrower name**
- **Source Chapter:** 11 — Designing smaller classes (section: Incomplete names hide responsibilities)
- **Principle:** Renaming a two-job class after only one of its jobs is *worse* than the honest compound name, because the name now promises less than the class does. This typically happens when a class grows new behaviour without being restructured.
- **Problem:** Readers trust the name and miss the hidden behaviour.
- **Detection Signals:**
  - Public methods outside what the class name promises (e.g. confirmation logic inside a notification sender).
  - Git history showing unrelated methods added while the name stayed the same.
- **Recommended Action:** Move the extra behaviour to its own class instead of hiding it under the old name.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-188, PCC-192.
- **Review Question:** Does every public method fit what the class name promises?

#### PCC-190
**Scrutinise vague catch-all names**
- **Source Chapter:** 11 — Designing smaller classes (section: Vague names are a warning sign)
- **Principle:** Manager, Handler, Helper, Service, Utility, Utils and Processor are not wrong in themselves. They need a closer look when nothing more specific can describe the class, because the vagueness can hide a catch-all. An appointments "manager" might confirm, notify, persist and validate.
- **Problem:** Unrelated behaviour accumulates under a name that excludes nothing.
- **Detection Signals:**
  - Class names ending in those suffixes whose member lists span several concerns.
  - The class shrinks to a simple role once something is extracted (`CustomerService` → `Customer`, see PCC-194).
- **Recommended Action:** Write down what the class actually does, look for cohesive groups, extract them, and rename the remainder specifically.
- **Exceptions/Trade-offs:** Explicitly "not inherently wrong". A cohesive class may keep such a name. A mandated *folder* name such as `Service/` is a location, not a class name; check the class's own name and content **(applied)**.
- **Related Rules:** PCC-186, PCC-194; ch2 Meaningful names.
- **Review Question:** Without the suffix, can you say precisely what this class does?

#### PCC-191
**Separate concepts from different abstraction levels that a name combines**
- **Source Chapter:** 11 — Designing smaller classes (section: Vague names are a warning sign; Table 11.1)
- **Principle:** A name that joins a high-level concern with a low-level mechanism (the book's `UserInterfaceStringProcessor`) shows orchestration and mechanics mixed in one class. They can collaborate without living together.
- **Problem:** Intent gets buried under mechanics, and the mechanics cannot be reused.
- **Detection Signals:** Names combining UI/domain words with technical words such as String, Regex, Byte, File or Parser **(applied)**.
- **Recommended Action:** Move the low-level behaviour behind a dedicated collaborator.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-195.
- **Review Question:** Does the class name combine a high-level concern with a low-level mechanism?

#### PCC-192
**Treat a class name that changes with every requirement as an unstable boundary**
- **Source Chapter:** 11 — Designing smaller classes (section: Table 11.1 — Naming signals)
- **Principle:** If a class's name has to change whenever requirements change, its responsibility boundary is unstable.
- **Problem:** Independent reasons to change are bundled together.
- **Detection Signals:**
  - Repeated renames in history.
  - Names that grow a new word with each feature.
- **Recommended Action:** Re-evaluate whether the independent change reasons should be separated.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-189, PCC-194.
- **Review Question:** Has this class been renamed, or needed renaming, with recent requirement changes?

#### PCC-193
**Find boundaries by clustering members on the data and dependencies they use**
- **Source Chapter:** 11 — Designing smaller classes (section: Finding boundaries through data and dependencies; Refactoring case study → Identifying the issues)
- **Principle:** The class body is stronger evidence than its name. If one group of methods consistently uses one set of fields or dependencies and another group uses a different set, the class may hold two concepts. Injected dependencies that have little to do with each other (a randomiser and file access) point the same way.
- **Problem:** Independent concepts are coupled in one type. The method grouping is a real dependency fact, not intuition.
- **Detection Signals:**
  - Methods partition by fields. In the book's user account, the name and birth-date methods never touch the username/password/last-login fields, and vice versa.
  - Constructor parameters each used by a disjoint subset of methods.
  - Dependencies from unrelated technical areas.
- **Recommended Action:** Rearrange members so each group sits together and the split becomes visible. Then extract each cluster (person vs. credentials/login state).
- **Exceptions/Trade-offs:** Keep members together when most behaviour works on the same cohesive state (Table 11.2).
- **Related Rules:** PCC-185, PCC-196, PCC-198; ch13 cohesion.
- **Review Question:** Do the methods fall into groups that share no fields and no collaborators?

#### PCC-194
**Extract behaviour that changes for its own business reasons, then simplify the remaining name**
- **Source Chapter:** 11 — Designing smaller classes (section: Grouping behavior that changes together → Extracting billing behavior from customer data)
- **Principle:** Change history shows responsibilities. Methods repeatedly modified for the same business reasons form a responsibility that deserves its own class, even if they use the same data as the rest. Billing changes with taxes, discounts and policies, while customer data rarely changes.
- **Problem:** Frequently changing rules sit beside stable data, so every policy change risks the stable part.
- **Detection Signals:**
  - `git log` shows one method or area changing far more often than the rest.
  - Change requests that touch only one subset.
  - Comments like "changes happen frequently here".
- **Recommended Action:** Extract the volatile part (`BillingCalculator`, which receives the orders as a parameter). Then rename the remainder to its now-simpler role (`CustomerService` → `Customer`).
- **Exceptions/Trade-offs:** If changes normally touch the same members together, keep them together (Table 11.2).
- **Related Rules:** PCC-190, PCC-192; ch5 SRP; ch6 OCP.
- **Review Question:** Does one part of this class change for reasons the rest of it never does?

#### PCC-195
**Keep a class at one level of abstraction; push mechanics into lower-level components**
- **Source Chapter:** 11 — Designing smaller classes (section: Separating levels of abstraction → Moving string-cleaning details out of blog-post storage)
- **Principle:** High-level methods say *what* the application does; low-level methods say *how* a technical operation works. Generic mechanics (regex collapsing repeated spaces) belong in a lower-level component (e.g. a string extension method). The high-level class then reads like the use case, and the mechanism becomes reusable.
- **Problem:** Intent gets buried under detail, and a generic operation is locked inside a class where nothing else can use it.
- **Detection Signals:**
  - A storage, orchestration or command class holding `Regex` fields, string parsing, array shuffling or similar algorithms in private methods.
  - Private helpers whose logic has nothing to do with the class's domain.
- **Recommended Action:** Extract the mechanism into a lower-level component (an extension method is acceptable for a pure string operation, PCC-171) and call it from the high-level method.
- **Exceptions/Trade-offs:** Keep members together when they describe one coherent concept at a consistent level (Table 11.2).
- **Related Rules:** PCC-191, PCC-198, PCC-168, PCC-171; PCC-204.
- **Review Question:** Does any private method here implement a generic mechanism unrelated to this class's purpose?

#### PCC-196
**Extract only when the new type is a coherent responsibility; avoid class proliferation**
- **Source Chapter:** 11 — Designing smaller classes (section: Avoiding unnecessary class proliferation; Table 11.2)
- **Principle:** More classes and more total lines are an acceptable price for granularity: a workflow can be read from collaborator names alone, and opening them becomes a choice (the book's account-storage class reads as validate, save, notify). But the goal is not "as many classes as possible". Avoid both extremes, the all-owning class and dozens of tiny types with no independent meaning.
- **Problem:** Under-extraction leads to god classes. Over-extraction scatters one idea across meaningless fragments.
- **Detection Signals:**
  - Over-extraction: tiny types with a trivial member, used by a single class, with no meaning on their own.
  - Under-extraction: see PCC-183.
- **Recommended Action:** Ask the six questions:
  1. Do the methods use the same data?
  2. Do they change for the same reasons?
  3. Are they at the same abstraction level?
  4. Can the new class be named clearly?
  5. Does extraction improve encapsulation?
  6. Would the component be useful or testable on its own?
  Extract only when the answers favour it.
- **Exceptions/Trade-offs:** This is the core trade-off of the chapter: granularity versus fragmentation.
- **Related Rules:** PCC-186, PCC-193, PCC-194, PCC-195, PCC-197, PCC-198; ch13 coupling/cohesion.
- **Review Question:** Does each newly extracted class own a responsibility that means something outside the class it came from?

#### PCC-197
**Don't extract when it forces private details into the open**
- **Source Chapter:** 11 — Designing smaller classes (section: Recognizing when a large class is acceptable; Table 11.2 — "Does extraction improve encapsulation?")
- **Principle:** A good extraction gives the new class ownership of the data it needs, or a clean abstraction to work through. If it can only work by exposing the original class's private implementation, keep the code together.
- **Problem:** Encapsulation is lost. Whatever the extracted class can reach, everyone can reach.
- **Detection Signals:**
  - An extraction diff that widens `private` to `internal`/`public`.
  - A new class taking the original class as a parameter to reach its internals.
  - `InternalsVisibleTo` added for the purpose **(applied)**.
- **Recommended Action:** Keep the code together, or redraw the boundary so the new class owns its own data.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-182, PCC-185, PCC-196.
- **Review Question:** After the extraction, is every formerly private member still private?

#### PCC-198
**Free reusable mechanics trapped inside domain classes**
- **Source Chapter:** 11 — Designing smaller classes (section: Refactoring case study → Identifying the issues; Improving the code; Understanding the dangers of large classes)
- **Principle:** When a generic capability (shuffling a collection, reading a data file) lives inside a domain class, other code cannot use it without creating that class. Making the method public does not help, because callers would still need an instance of the domain class first. Move it into its own class.
- **Problem:** Reuse is blocked or forces heavy object construction. The capability cannot be tested on its own.
- **Detection Signals:**
  - Generic `<T>` methods or collection/string utilities inside domain classes.
  - Unrelated features instantiating a heavy class just to call one method **(applied)**.
- **Recommended Action:** Extract it into a dedicated class (`CollectionRandomizer`) with an interface, move its dependency (`IRandom`) along, and inject it where it is used.
- **Exceptions/Trade-offs:** Keep it in place if it has no meaning outside the original class and would exist only to cut lines (Table 11.2).
- **Related Rules:** PCC-195, PCC-196, PCC-200.
- **Review Question:** Is there a reusable mechanism here that other code could reach only by constructing this class?

#### PCC-199
**Move a method to the parameter type whose knowledge it encodes**
- **Source Chapter:** 11 — Designing smaller classes (section: Refactoring case study → Improving the code)
- **Principle:** To find where a method belongs, look at its parameters; it often belongs on one of them. Whether an ingredient suits a diet is knowledge for `Ingredient`, not for the pizza generator.
- **Problem:** Domain knowledge sits in the wrong class, so that class collects responsibilities that are not its own.
- **Detection Signals:** Private methods that take an object and mostly reason about that object's properties. The book does not name this smell.
- **Recommended Action:** Move the method onto that parameter's type and have the caller invoke it on the object (`ingredient.IsOkInDiet(diet)`).
- **Exceptions/Trade-offs:** Parameters that cannot host behaviour, such as an enum (`DietOption`), are excluded.
- **Related Rules:** PCC-193, PCC-196; ch13 cohesion.
- **Review Question:** Does this method mainly reason about one of its parameters rather than its own class?

#### PCC-200
**Give an extracted collaborator an abstraction, inject it, and move its exclusive dependencies with it**
- **Source Chapter:** 11 — Designing smaller classes (section: Refactoring case study → Improving the code)
- **Principle:** When extracting a responsibility (ingredient reading):
  1. Make the moved method public on the new class.
  2. Give the class an interface, so consumers depend on an abstraction (DIP).
  3. Move the dependencies and constants that only it uses (`IFileAccess`, the file name).
  4. Inject the interface into the original class.
  
  The original then has one responsibility, and the data source can change without touching it.
- **Problem:** Leftover fields, constants and concrete dependencies keep the classes coupled and stop the source from being swapped.
- **Detection Signals:**
  - After a split, the original class still holds fields or constants only the extracted class uses.
  - The consumer `new`s the extracted class or takes its concrete type.
  - The extracted class has no interface although consumers need to swap or mock it.
- **Recommended Action:** Follow the steps above. Stateless private parsing helpers inside the new class can become `static` (PCC-168).
- **Exceptions/Trade-offs:** When an interface is unnecessary is (not stated in this chapter).
- **Related Rules:** PCC-168, PCC-180, PCC-198; ch9 DIP.
- **Review Question:** After the split, does each class hold only the dependencies it uses, and does the consumer see only an interface?

#### PCC-201
**Keep a refactoring behaviour-preserving**
- **Source Chapter:** 11 — Designing smaller classes (section: Refactoring case study → Improving the code — note on error handling)
- **Principle:** While extracting the ingredient reader, the author leaves the known lack of error handling in place: adding it would go beyond a refactoring. The gap is acknowledged as not ideal.
- **Problem:** (implied) Mixing new behaviour into a structural change makes it harder to see that the structure changed safely.
- **Detection Signals:**
  - "Refactor" PRs that also add validation, error handling or new behaviour.
  - Moved code whose semantics changed during the move.
- **Recommended Action:** Do the structural change on its own. Note known gaps and address them as a separate change.
- **Exceptions/Trade-offs:** The gap should not be forgotten. The author explicitly calls it not ideal.
- **Related Rules:** PCC-213, PCC-218.
- **Review Question:** Does this refactoring change only structure, leaving behaviour changes for a separate step?

### Code smells & anti-patterns catalogue

| Name | Signal | Rule IDs |
|---|---|---|
| God class / god object | Touched by every change; thousands of lines; dozens of members or constructor parameters; poor tests | PCC-183 |
| Forced size split | `XxxHelper` / `XxxUtils` created only to cut length; pair calls each other | PCC-184, PCC-186 |
| Clever hack | Simple loop compressed into a dense expression "to save lines" | PCC-187 |
| Compound "And" name | `AbcAndXyz` class names | PCC-188 |
| Incomplete name | Public methods outside what the name promises | PCC-189 |
| Catch-all name | Manager/Handler/Helper/Service/Utility/Utils/Processor covering many concerns | PCC-190 |
| Mixed-abstraction name/class | Name or body mixes UI/domain intent with string/regex/file mechanics | PCC-191, PCC-195 |
| Unstable name | Class renamed or extended in name with each requirement | PCC-192 |
| Disjoint field/dependency clusters | Method groups share no fields; unrelated injected dependencies | PCC-193 |
| Volatile responsibility | One method area changes far more often for its own business reasons | PCC-194 |
| Trapped reusable mechanism | Generic utility reachable only through a domain class | PCC-198 |
| Misplaced knowledge | Method mostly reasons about a parameter's data | PCC-199 |
| Class proliferation | Many tiny types with no independent meaning | PCC-196 |
| Encapsulation-breaking extraction | Extraction widens private members' visibility | PCC-197 |

### Refactoring techniques named in the chapter
- Apply SRP: extract each cohesive responsibility into its own class.
- Rearrange members so field-usage clusters sit together, then extract each cluster (personal data vs. account credentials).
- Extract the frequently changing part (billing calculation) and rename the remainder to its simpler role.
- Extract low-level mechanics into a lower-level component, e.g. a string extension method.
- Extract class + introduce interface + constructor injection (ingredients repository, collection randomiser), moving exclusive dependencies and constants along.
- Move a method onto one of its parameter types (diet check onto `Ingredient`).
- Extract a stateless private helper and make it static (building one ingredient from a line).
- Rename a class after extraction so the name matches its now-narrower responsibility.

### Things the author says NOT to do mechanically
- Don't split a class just to get under a line limit. Size is a warning sign, not a failure, and forced splits can do more harm than the length.
- Don't split a large class whose members all serve one purpose (the `List<T>` case), especially if the split would expose private data.
- Don't extract `<Original>Helper` classes that cut one responsibility in two.
- Don't compress readable code into clever one-liners. Fewer lines is not the goal, and good refactoring often adds lines.
- Don't treat Manager/Service/Helper-style names as wrong in themselves. They are a prompt to scrutinise.
- Don't maximise class count. Extract only types with a coherent, independently meaningful responsibility.
- Don't slip new behaviour (such as error handling) into a refactoring.
- Do remember the caveat that large classes are justified only *rarely*. The default suspicion toward big classes stands.

---

## Chapter 12 — Organizing classes and projects

### Chapter summary
- A code base of well-behaved classes can still be hard to navigate if things are not where people look for them: members in random order, several types per file, folders that say nothing about the application.
- Inside a class, order members by what a reader needs first: fields/properties (state), constructors, public methods, then private implementation. Callers sit above callees so the order shows the flow. These are recommendations; a consistently applied team convention wins.
- By default each type lives in its own file bearing its name. This helps navigation, makes version-control history informative and reduces merge conflicts. It is not dogma: tiny, related, stable types may share a file.
- Folder structure: follow the framework's template where it sets one. Beyond that, organise by technical concern or by feature, whichever fits the application and the team's mental model, and stay consistent. Add subfolders when a folder grows crowded or mixed. Structure can emerge and be reorganised later.
- Keep file moves in their own commits. Keep tests in a separate project that mirrors production.
- A class is a small API: similar methods must handle the same situation the same way (not-found: throw vs. null vs. bool/out), and the same kind of operation gets the same verb. Write down conventions that settle recurring questions, including test naming.
- Restructure in small, separately reviewable steps (members → files → folders → consistency check), never mixed with behaviour changes.

### Rules

#### PCC-202
**Order class members by what a reader looks for first**
- **Source Chapter:** 12 — Organizing classes and projects (section: Ordering class members for readability; Table 12.1)
- **Principle:** The compiler does not care about order; the reader does. A predictable order cuts the searching needed to learn what a class holds, how it is created, what it depends on and what it offers:
  1. Fields and properties first, because they describe what the class is made of.
  2. Constructors next to the data they initialise.
  3. Public methods before private ones.
  4. Private implementation lower down.
- **Problem:** Readers hunt through the file to answer basic questions about the class.
- **Detection Signals:**
  - Fields declared mid-class or at the bottom.
  - The constructor buried below methods.
  - Private helpers above the public API.
  - Different orders in different classes of the same project.
- **Recommended Action:** Reorder to fields/properties → constructors → public methods → private methods, the same way in every class.
- **Exceptions/Trade-offs:** These are recommendations, not rigid rules (see PCC-206).
- **Related Rules:** PCC-203–PCC-206; ch4 Formatting code.
- **Review Question:** Can a reader see the state, the construction and the public API without scrolling past implementation details?

#### PCC-203
**Group dependencies and other private fields together near the top**
- **Source Chapter:** 12 — Organizing classes and projects (section: Ordering class members for readability; Table 12.1)
- **Principle:** Dependencies are important context. Group fields consistently near the top so no declaration surprises the reader later in the class.
- **Problem:** A dependency declared halfway down is easy to miss, which partly hides the class's dependency graph.
- **Detection Signals:**
  - Injected `private readonly I…` fields scattered between methods.
  - A new field added next to the method that uses it rather than with the other fields.
- **Recommended Action:** Move all fields into one consistently ordered block at the top.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-202, PCC-175.
- **Review Question:** Are all fields, injected dependencies especially, declared together at the top?

#### PCC-204
**Place callers above the methods they call**
- **Source Chapter:** 12 — Organizing classes and projects (section: Ordering class members for readability; Table 12.1 — private methods row)
- **Principle:** Member order can show flow. Putting a calling method above the one it uses lets the reader move from high-level intent down to detail. Private methods sit below the public behaviour they support.
- **Problem:** Readers meet detail before they know what it is for.
- **Detection Signals:**
  - A private helper defined above the public method that calls it.
  - Helpers placed far away from their only caller.
- **Recommended Action:** Reorder top-down: public operation, then the helpers it calls.
- **Exceptions/Trade-offs:** A recommendation. Consistency with the team's convention takes priority.
- **Related Rules:** PCC-202, PCC-195; ch4 Formatting code.
- **Review Question:** Reading top-down, does each method appear before the helpers it calls?

#### PCC-205
**Position nested types by their accessibility**
- **Source Chapter:** 12 — Organizing classes and projects (section: Ordering class members for readability; Table 12.1 — nested types row)
- **Principle:** Accessibility decides prominence. Public nested types go near the public surface, private nested types near the bottom.
- **Problem:** (not stated in book beyond prominence)
- **Detection Signals:**
  - Private nested classes, records or enums at the top of a class.
  - Public nested types buried at the end.
- **Recommended Action:** Move them according to their accessibility.
- **Exceptions/Trade-offs:** A recommendation, not a rigid rule.
- **Related Rules:** PCC-202.
- **Review Question:** Are public nested types near the public API and private ones at the bottom?

#### PCC-206
**A consistently applied team convention beats personal preference**
- **Source Chapter:** 12 — Organizing classes and projects (section: Ordering class members for readability)
- **Principle:** A team convention that is applied consistently is worth more than repeatedly rearranging code to fit one person's taste.
- **Problem:** Preference-driven reordering creates churn and inconsistency.
- **Detection Signals:**
  - PRs that only reorder members against the established project convention.
  - Back-and-forth reordering diffs.
- **Recommended Action:** Agree on a convention, record it (PCC-217) and follow it.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-202, PCC-217.
- **Review Question:** Does this change follow the team's agreed order rather than a personal one?

#### PCC-207
**Put one type in one file named after the type**
- **Source Chapter:** 12 — Organizing classes and projects (section: Organizing classes into separate files)
- **Principle:** By default each type gets its own file, and the file carries the type's name (`DatabaseConnector.cs`). Benefits:
  - Types are easy to find, and the file list explains the project.
  - Version-control history is informative: changed files show which types were affected.
  - Merge conflicts are rarer.
- **Problem:** With many types in one file, history can only report "that file changed", conflicts are more likely, and types are hard to locate.
- **Detection Signals:**
  - Files with several unrelated top-level types.
  - File names that don't match the type name.
  - Grab-bag files such as `Models.cs`, `Helpers.cs` or `Misc.cs` **(applied)**.
- **Recommended Action:** Split the file and name each new file after its type.
- **Exceptions/Trade-offs:** See PCC-208.
- **Related Rules:** PCC-208, PCC-213, PCC-218.
- **Review Question:** Could someone find each type by guessing its file name on the first try?

#### PCC-208
**Let tiny, related, stable types share a file**
- **Source Chapter:** 12 — Organizing classes and projects (section: Organizing classes into separate files)
- **Principle:** One type per file should not become dogma. Very small types that clearly belong together, such as day/month/season enums in one `DateEnums.cs`, can share a file without making anything harder to find.
- **Problem:** Applied rigidly, the rule produces a pile of trivial files without helping anyone navigate.
- **Detection Signals (to accept):**
  - Each type is a handful of lines.
  - They are unlikely to change.
  - Someone looking for one would naturally look for the others in the same place.
- **Recommended Action:** Allow grouping under a file name that names the shared concept.
- **Exceptions/Trade-offs:** Does not extend to substantial or unrelated types.
- **Related Rules:** PCC-207.
- **Review Question:** Would someone looking for any of these types naturally look in this file?

#### PCC-209
**Follow the framework's established folder layout**
- **Source Chapter:** 12 — Organizing classes and projects (section: Structuring project files and folders)
- **Principle:** When a framework template creates folders with established purposes, follow that layout. Developers who know the framework know where to look, tooling and documentation assume the structure, and the framework may require files in particular places.
- **Problem:** Deviating confuses newcomers and tools, and can break framework expectations.
- **Detection Signals:**
  - Files moved out of template-defined locations.
  - Custom folders duplicating the purpose of template folders.
- **Recommended Action:** Keep the template layout and add your own organisation inside or beyond it.
- **Exceptions/Trade-offs:** Beyond what the framework dictates, folders should reflect the application's architecture (PCC-210).
- **Related Rules:** PCC-210.
- **Review Question:** Does this change respect the folder layout the framework/template expects?

#### PCC-210
**Organise folders by technical concern or by feature, and apply the choice consistently**
- **Source Chapter:** 12 — Organizing classes and projects (section: Structuring project files and folders; Table 12.2)
- **Principle:** No single layout suits every application. The two common models are both valid:
  - **By concern** (DataAccess, UI, BusinessLogic, Models): makes layers explicit, but one feature spreads across several folders.
  - **By feature** (Login, ItemSearch, ShoppingCart): keeps one capability together, but each folder holds several concerns.
  
  Choose the one that matches the application and the way the team thinks about it, then stay consistent.
- **Problem:** Mixed or arbitrary structures stop folders from saying what the application is made of.
- **Detection Signals:**
  - Feature folders and layer folders mixed at the same level for no reason.
  - A new file placed against the project's chosen model.
  - A feature's files scattered when the team reasons in features (or the reverse).
- **Recommended Action:** Pick the model, record it, and place new code accordingly. Feature folders may contain concern subfolders (Table 12.2 notes each feature folder holds several technical concerns). A feature-folder convention with Model/Service/View/ViewModel subfolders is one such hybrid **(applied)**.
- **Exceptions/Trade-offs:** The book prefers neither model; fit and consistency decide.
- **Related Rules:** PCC-209, PCC-211, PCC-212.
- **Review Question:** Does this new file follow the folder model the rest of the project uses?

#### PCC-211
**Split a crowded or mixed folder into subfolders**
- **Source Chapter:** 12 — Organizing classes and projects (section: Structuring project files and folders)
- **Principle:** When a folder gets crowded, or starts holding unrelated things, introduce subfolders (e.g. DataAccess → Database, Cloud, FileSystem) once each part has enough code to justify it.
- **Problem:** Large mixed folders hide structure.
- **Detection Signals:**
  - A folder with many files covering distinct sub-topics.
  - Files that are unrelated to one another sharing a folder.
- **Recommended Action:** Create subfolders by sub-topic and move the files (in a move-only commit, PCC-213).
- **Exceptions/Trade-offs:** Wait until each subgroup has enough code to be worth separating.
- **Related Rules:** PCC-210, PCC-213.
- **Review Question:** Does this folder now hold distinct groups large enough to deserve their own subfolders?

#### PCC-212
**Let structure emerge, and reorganise without hesitation**
- **Source Chapter:** 12 — Organizing classes and projects (section: Structuring project files and folders)
- **Principle:** The right structure often emerges gradually instead of being designed up front, so reorganising later is normal and should not be avoided.
- **Problem:** (implied) A structure frozen at the start drifts away from how the application and team actually work.
- **Detection Signals:**
  - A folder layout that no longer matches how the team describes the system.
  - Reluctance to move files despite navigation pain **(applied)**.
- **Recommended Action:** Reorganise when the structure stops fitting, following PCC-213 and PCC-218.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-210, PCC-213, PCC-218.
- **Review Question:** Does the current structure still reflect how the team thinks about the application?

#### PCC-213
**Commit file moves separately from code changes**
- **Source Chapter:** 12 — Organizing classes and projects (section: Structuring project files and folders; Refactoring project and class structure)
- **Principle:** Keep file moves in their own commits, separate from edits to the files' contents.
- **Problem:** A combined commit produces a large, hard-to-read diff, and the change that deserves review gets lost among files that only moved.
- **Detection Signals:**
  - Commits that both rename/move files and change their logic.
  - Git showing delete + add instead of a rename because the content changed too much **(applied)**.
- **Recommended Action:** Make one move-only commit, then a separate commit for the code changes. In C#, a namespace update that mirrors the new folder is part of the mechanical move **(applied; not stated in book)**.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-211, PCC-212, PCC-218, PCC-201.
- **Review Question:** Does any commit both move files and change their behaviour?

#### PCC-214
**Keep tests in a separate project that mirrors the production structure**
- **Source Chapter:** 12 — Organizing classes and projects (section: Structuring project files and folders)
- **Principle:** Test code goes in its own project so it is not built or shipped with the application. Mirroring the production project's structure makes each class's tests easy to find without learning a second scheme.
- **Problem:** Tests shipped with the product, or tests that are hard to locate.
- **Detection Signals:**
  - Test classes inside production projects.
  - Test folder layout diverging from production.
  - Test files not traceable to the class they cover.
- **Recommended Action:** Use a separate test project with folders that mirror production.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-210; ch15 Testable code and clean tests.
- **Review Question:** Can the tests for a given class be found by following the production folder path in the test project?

#### PCC-215
**Handle the same situation the same way within a type**
- **Source Chapter:** 12 — Organizing classes and projects (section: Maintaining consistency within classes)
- **Principle:** A class is a small API, and its methods should not surprise callers. In the book's people-lookup class, three lookups signal "not found" three ways: one throws, one prints to the console and returns null, one returns `false` with an `out` result. Pick one convention suited to the application and use it throughout the type.
- **Problem:** A caller who learns one method assumes the rest behave the same, and is wrong. Each method has to be looked up separately. The inconsistency spreads to every caller as a mix of try/catch, null checks and bool checks.
- **Detection Signals:**
  - Sibling methods (`GetBy*`) that differ in failure signalling: throw vs. return null vs. `bool` + `out`.
  - Console or log output inside data-access methods standing in for error reporting.
  - Inconsistent nullable return types among similar methods.
- **Recommended Action:** Unify on one convention. The book's refactor throws `KeyNotFoundException` with a specific message in every lookup.
- **Exceptions/Trade-offs:** The book does not mandate *which* convention, only that it suits the application and is applied consistently.
- **Related Rules:** PCC-216, PCC-217; ch3 Writing better methods.
- **Review Question:** Do all methods of this type report the same kind of failure in the same way?

#### PCC-216
**Use one name for one kind of operation**
- **Source Chapter:** 12 — Organizing classes and projects (section: Maintaining consistency within classes; Refactoring project and class structure)
- **Principle:** Don't mix `Get` and `Fetch` for one kind of operation unless the two words deliberately mean different things. Across classes, the same kind of operation carries the same name, and similar operations behave the same way.
- **Problem:** Synonym verbs make readers guess whether a different word means different behaviour.
- **Detection Signals:**
  - Equivalent operations named `Get…`, `Fetch…`, `Retrieve…`, `Load…` across the touched area (the last two **(applied)**).
  - Similar operations in sibling classes behaving differently.
- **Recommended Action:** Rename to the agreed verb, and align behaviour.
- **Exceptions/Trade-offs:** Different verbs are fine when the difference is deliberate and meaningful.
- **Related Rules:** PCC-215, PCC-217; ch2 Meaningful names.
- **Review Question:** Does each kind of operation use the same verb everywhere in this area?

#### PCC-217
**Write down conventions that keep settling the same question**
- **Source Chapter:** 12 — Organizing classes and projects (section: Maintaining consistency within classes; Refactoring project and class structure)
- **Principle:** When a convention settles a question that keeps coming back (failure signalling, operation verbs, unit-test naming), record it so reading anyone's code holds no surprises.
- **Problem:** The same debate repeats in review, and code diverges between authors.
- **Detection Signals:**
  - Recurring review threads on the same topic.
  - Several unit-test naming styles in one test project.
- **Recommended Action:** Add the convention to the project's written standards (e.g. a code-standards document **(applied)**).
- **Exceptions/Trade-offs:** Record conventions that settle *recurring* questions, not every preference.
- **Related Rules:** PCC-206, PCC-215, PCC-216; ch15 test naming.
- **Review Question:** Is the convention this change relies on written down, and if this question keeps recurring, should it be?

#### PCC-218
**Restructure in small, reviewable steps, from members outwards**
- **Source Chapter:** 12 — Organizing classes and projects (section: Refactoring project and class structure)
- **Principle:** Reorganise a hard-to-navigate project in small steps that can each be reviewed on their own, never mixing structural moves with behaviour changes. Sequence:
  1. Member order in every class.
  2. One type per file, named after the type.
  3. A folder model applied consistently, with subfolders where crowded, a mirrored test project, and move-only commits.
  4. A comparison of the classes in the touched area for consistent behaviour and names, writing down conventions that recur.
- **Problem:** Big mixed reorganisations are unreviewable and hide behaviour changes.
- **Detection Signals:**
  - One PR that reorders members, moves files and changes logic together.
  - A reorganisation with no consistency pass.
- **Recommended Action:** Follow the sequence above, one reviewable step at a time.
- **Exceptions/Trade-offs:** The chapter presents these choices as guidance, not rigid rules, and warns against adding unnecessary structural complexity.
- **Related Rules:** PCC-202, PCC-207, PCC-210, PCC-213, PCC-215, PCC-217, PCC-201.
- **Review Question:** Can each step of this restructuring be reviewed on its own, with no behaviour change mixed in?

### Code smells & anti-patterns catalogue

| Name | Signal | Rule IDs |
|---|---|---|
| Unpredictable member order | Fields/constructors buried; private helpers above public API; order varies by class | PCC-202, PCC-205 |
| Scattered fields/dependencies | Field declarations interleaved with methods | PCC-203 |
| Bottom-up flow | Helpers appear before the public methods that use them | PCC-204 |
| Preference churn | Reorder-only diffs against the team convention | PCC-206 |
| Multi-type grab-bag file | Several unrelated types in one file; file name ≠ type name | PCC-207 |
| Framework-layout deviation | Files moved out of template-defined folders | PCC-209 |
| Mixed folder model | Feature and layer folders mixed arbitrarily | PCC-210 |
| Crowded / mixed folder | Many unrelated files in one folder | PCC-211 |
| Move + edit commit | Renames/moves and logic changes in the same commit | PCC-213, PCC-218 |
| Tests in production project | Test code built/shipped with the app; unmirrored layout | PCC-214 |
| Inconsistent failure signalling | Throw vs. null vs. bool/out across sibling methods; console output as error reporting | PCC-215 |
| Synonym verbs | `Get` vs. `Fetch` for the same operation | PCC-216 |
| Unwritten recurring convention | Same review debate repeats; mixed test-naming styles | PCC-217 |

### Refactoring techniques named in the chapter
- Reorder members: fields/properties → constructors → public methods → private methods. Callers above callees. Nested types by accessibility.
- Split multi-type files into one type per file named after the type, except for tiny related stable types.
- Adopt the framework layout, then a concern- or feature-based folder model applied consistently.
- Introduce subfolders when a folder is crowded or mixed.
- Commit file moves separately from code edits.
- Move tests into a separate project that mirrors production.
- Unify failure signalling across a type's methods (e.g. always throw `KeyNotFoundException` with a specific message).
- Unify operation verbs (`Get` vs. `Fetch`) and write recurring conventions down, including unit-test naming.
- Restructure step by step: inside classes → files → folders → consistency comparison.

### Things the author says NOT to do mechanically
- Member ordering is a recommendation, not a rule. Don't keep reordering code to personal taste; follow the team convention.
- One type per file is a default, not dogma. Tiny related stable types (e.g. date enums) may share a file.
- There is no universally correct folder layout. By-concern and by-feature can both be clean, and framework templates take precedence where they apply.
- Don't try to design the final structure up front. It emerges, and reorganising later is fine, in move-only commits.
- Don't force a single verb where `Get` and `Fetch` deliberately mean different things.
- Don't document every preference. Write down conventions that settle questions that keep recurring.
- Don't add structural complexity for its own sake when reorganising.

---

## Chapter 13 — Balancing coupling, cohesion, and reuse

### Chapter summary
- Small, tidy classes can still form a rigid system when they know too much about each other, when unrelated things share a type, or when de-duplication produces abstractions harder to follow than the copies they replaced.
- Coupling = how much one class must know about another; measured by how far a change on one side forces changes on the other. Dependency itself is normal; the problem is knowledge of private details and reliance on concrete types where an abstraction would do.
- The Law of Demeter (least knowledge) limits coupling: talk to what you receive, own, or create; ask for the thing you need instead of something to dig through.
- Cohesion = how strongly members of a class belong together. It is less something you build than something you must avoid breaking; it is related to, but not the same as, SRP — a very large class (List<T>) can be perfectly cohesive.
- DRY is about *knowledge*, not text: each business rule needs one authoritative representation. Mechanical duplication is a lesser, separate concern; coincidental duplication (same value, different rule) should usually be kept.
- Speculative abstraction is costly: let requirements mature, then extract. BDUF names the trap; YAGNI and KISS guard against it — none of them forbids planning or abstraction. An interface over a *needed* capability whose implementation is undecided is legitimate.
- For sharing/varying behavior, composition usually beats inheritance (looser coupling, reusable parts, runtime choice, flat structure, easier tests, easier ORM mapping); inheritance stays fine for framework contracts, exception types, and small closed data families.
- The VacationPlanner case study applies everything: inject an abstraction instead of `new`-ing a concrete provider, wrap static Console behind an interface, move reporting out of the planner, define weekend knowledge once and in the class that owns calendar knowledge, use one term per concept, then tidy formatting/files/member order.

### Rules

#### PCC-219
**Expose what the consumer needs, not how the data is stored**
- **Source Chapter:** 13 — Balancing coupling, cohesion, and reuse (section: Understanding and reducing coupling between classes)
- **Principle:** A class should hide its internal storage choice and publish only the capability its consumers use — e.g. an iterable sequence (`IEnumerable<T>`) instead of the concrete array/list it keeps.
- **Problem:** A consumer that relies on `.Length`, indexing, or the concrete collection type breaks when the owner swaps array → `HashSet`, even though the owner's responsibility did not change. Internal changes leak into other classes.
- **Detection Signals:**
  - Public property typed as `T[]`, `List<T>`, `Dictionary<K,V>`, `HashSet<T>` that returns the private backing field.
  - Consumers using `obj.Items.Length`, `obj.Items.Count`, `obj.Items[i]` in `for` loops over another object's collection.
  - Replacing a collection type inside one class requires edits in another class.
- **Recommended Action:** Make the backing field private; expose `IEnumerable<T>` (or the narrowest interface that the consumer actually needs); change consumers to `foreach`/LINQ over the sequence.
- **Exceptions/Trade-offs:** The book frames this as exposing "only what the consumer needs" — if a consumer genuinely needs count or indexed access, a narrower-but-sufficient contract is implied; the book does not enumerate alternatives (not stated in book).
- **Related Rules:** PCC-220, PCC-222, PCC-248; ch8 ISP.
- **Review Question:** Could the owning class change its internal collection type without any consumer being edited?

#### PCC-220
**Never let a consumer modify another object's internal data**
- **Source Chapter:** 13 (section: Understanding and reducing coupling between classes; Table 13.1)
- **Principle:** If one class can null out or replace elements inside another class's state, encapsulation is broken. Expose operations or a read-only view instead.
- **Problem:** Implementation choices leak outward and any consumer can corrupt the owner's state.
- **Detection Signals:**
  - Index assignment through a getter: `other.Items[i] = …`, `other.Items[i] = null`.
  - Getter-only properties returning mutable arrays/lists (the getter does not protect the contents).
  - Consumers calling `.Add/.Remove/.Clear` on a collection obtained from another object's property.
- **Recommended Action:** Return a read-only view (`IEnumerable<T>` in the book's example) and add explicit operations on the owner for legitimate mutations.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-219, PCC-225.
- **Review Question:** Can any code outside this class change the contents of its internal collections?

#### PCC-221
**Depend on an abstraction where the collaborator is expected to vary**
- **Source Chapter:** 13 (section: Understanding and reducing coupling between classes; Table 13.1 "depends directly on a concrete implementation")
- **Principle:** When replacing or changing a collaborator is plausible, the consumer should depend on an interface or other suitable abstraction rather than the concrete class.
- **Problem:** Swapping or changing the concrete collaborator forces changes in every consumer.
- **Detection Signals:**
  - Fields, constructor parameters, or method parameters typed as concrete service classes where alternative implementations are realistic (data sources, output channels, country/regional rules).
  - Type names with a variant baked in (e.g. a `Polish…Provider`) used directly by core logic.
- **Recommended Action:** Introduce an interface describing what the consumer needs; type the dependency as that interface; supply the concrete type from outside (see PCC-244).
- **Exceptions/Trade-offs:** The book's table qualifies it: "when variation is expected". Some dependency is unavoidable and not a problem in itself; do not wrap every class in an interface speculatively (PCC-236).
- **Related Rules:** PCC-244, PCC-236, PCC-239; ch9 DIP.
- **Review Question:** Is there a realistic second implementation of this collaborator, and if so does the consumer depend only on its abstraction?

#### PCC-222
**When a small change cascades, hide the volatile detail behind a stable contract**
- **Source Chapter:** 13 (section: Understanding and reducing coupling between classes; Table 13.1)
- **Principle:** A small implementation change that forces edits in several unrelated classes shows that those classes know details that should have stayed local to one type.
- **Problem:** Fragile system; every internal change ripples.
- **Detection Signals:**
  - A diff that changes one internal detail (storage type, data shape, format) and touches several unrelated files to compensate.
  - Many classes reading the same low-level fields/shape of another type.
- **Recommended Action:** Identify the volatile detail, encapsulate it in its owning type, and give consumers a stable contract (method or interface) that does not change when the detail does.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-219, PCC-225, PCC-221.
- **Review Question:** Did this change to one class's internals force edits in classes that should not care?

#### PCC-223
**A class that needs many others set up to be tested carries too much knowledge**
- **Source Chapter:** 13 (section: Understanding and reducing coupling between classes; Table 13.1)
- **Principle:** If exercising one class requires constructing several unrelated collaborators, it is over-coupled and probably over-responsible.
- **Problem:** The class cannot be tested independently; its knowledge is excessive.
- **Detection Signals:**
  - Test arrange blocks building large object graphs for one unit.
  - Constructors with many concrete dependencies, or classes that create their collaborators internally.
- **Recommended Action:** Reduce the class's responsibilities and inject small, focused collaborators (abstractions that can be replaced in tests).
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-227, PCC-242, PCC-244; ch11 smaller classes; ch15 testable code.
- **Review Question:** Can this class be tested with only its direct, focused collaborators (or test doubles of them)?

#### PCC-224
**Do not inspect concrete subtypes to decide behavior; put the behavior behind the abstraction**
- **Source Chapter:** 13 (section: Understanding and reducing coupling between classes; Table 13.1)
- **Principle:** A client that must check which concrete subtype it has shows the abstraction is not carrying the behavior the client needs.
- **Problem:** Client is coupled to every subtype; the abstraction is incomplete.
- **Detection Signals:**
  - `is`/`as`/`switch` on type patterns, `GetType() ==`, `typeof(...)` comparisons against implementations of a shared interface/base.
  - Downcasts from an interface to a concrete class to reach a member.
- **Recommended Action:** Move the varying behavior into the abstraction (a member each implementation provides) or redesign the contract.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-239; ch7 LSP; ch6 OCP.
- **Review Question:** Does any client branch on the concrete type of something it received as an abstraction?

#### PCC-225
**Law of Demeter: do not navigate through chains of other objects' internals**
- **Source Chapter:** 13 (section: Applying the Law of Demeter)
- **Principle:** A class should work with objects it directly receives, owns, or creates, not reach through one object to traverse a chain of unrelated internals. Every link in a chain is another class you now depend on.
- **Problem:** A change anywhere along the chain (the shape of any intermediate return type) can break a class that only needed one value.
- **Detection Signals:**
  - Multi-hop navigation on domain/service objects: `a.GetB().GetC().Value`, `context.X().Y().Z`.
  - Constructors or methods whose first act is walking a chain to extract a value.
  - **[add-in illustration]** `uiapp.ActiveUIDocument.Document.ActiveView…` style traversal deep inside services instead of receiving the `Document`/`View` they use.
- **Recommended Action:** Ask for the end value (or a focused collaborator) directly as a parameter; let the caller do the navigation once, at the composition boundary.
- **Exceptions/Trade-offs:** The book gives no explicit exemption for data structures, value types, or fluent/LINQ APIs (not stated in book). Observation only: the book's own approved code chains calls on a `DateTime` from a data object (`Start.AddDays(...).ToShortDateString()`) and uses LINQ chains (`Select(...).Distinct()`), so it does not treat chained calls on values or query pipelines as violations. Apply the rule to navigation through *other objects' internals*, not to every dot.
- **Related Rules:** PCC-226, PCC-222; ch9 DIP.
- **Review Question:** Does this class depend on the internal structure of objects it did not receive, own, or create?

#### PCC-226
**Ask for the value you need, not for a container you can dig through**
- **Source Chapter:** 13 (section: Applying the Law of Demeter)
- **Principle:** If a constructor takes a broad context object only to extract one value, pass that value instead. The real dependency becomes visible.
- **Problem:** The class becomes coupled to the whole context type and its sub-objects; it is harder to reuse and test and is affected by unrelated changes.
- **Detection Signals:**
  - Parameter of a large "context"/"application"/"settings" type used for a single field or path.
  - Constructor body that only reads one deep property of a parameter and discards the rest.
  - Tests forced to build a full context object to supply one string.
- **Recommended Action:** Change the signature to accept the specific value (e.g. a directory string) or a narrow abstraction; push extraction to the caller.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-225, PCC-223; ch8 ISP; ch15 testability.
- **Review Question:** Does each parameter correspond to something the class actually uses, rather than something it digs through?

#### PCC-227
**Split a class along the seam where member groups share no state**
- **Source Chapter:** 13 (section: Designing for high cohesion)
- **Principle:** In a cohesive class, most members use the same state or related collaborators toward one concept. When two groups of members use disjoint fields/dependencies and meet at a single hand-off point, that point is the seam to split along.
- **Problem:** Unrelated responsibilities (e.g. pricing and owner notification) bundled together; the class is hard to name, test, and change.
- **Detection Signals:**
  - Fields used only by one subset of methods while another subset uses a different set of fields/injected services.
  - A class combining calculation with I/O or messaging (database lookup + notification + computation).
  - One group's only use of the other is calling a single method to obtain a value.
- **Recommended Action:** Extract each group into its own class (e.g. a pricer and a notifier); pass the hand-off value (price) as a parameter between them.
- **Exceptions/Trade-offs:** A class with no natural split point should not be split (PCC-229).
- **Related Rules:** PCC-228, PCC-229, PCC-246; ch5 SRP; ch11 smaller classes.
- **Review Question:** Are there groups of members in this class that use none of each other's fields or dependencies?

#### PCC-228
**Use the class name as a cohesion test**
- **Source Chapter:** 13 (section: Designing for high cohesion; Table 13.2)
- **Principle:** A highly cohesive type can be described precisely by one name; low cohesion shows up as a vague, compound, or misleading name.
- **Problem:** A name that cannot describe the whole type signals mixed concerns.
- **Detection Signals:**
  - Compound names joining two concerns; names that describe only part of what the class does.
  - Difficulty choosing a name during review; names that mislead about side effects (a "pricer" that sends messages).
- **Recommended Action:** Split the type (PCC-227) until each piece has a precise name.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-227; ch2 Meaningful names.
- **Review Question:** Does the class name accurately describe everything the class does?

#### PCC-229
**Do not destroy cohesion by over-splitting; size alone is not low cohesion**
- **Source Chapter:** 13 (section: Designing for high cohesion)
- **Principle:** High cohesion and SRP are related but distinct. Splitting every method into its own class satisfies an overly literal SRP and destroys cohesion. A large class can be highly cohesive when all members serve one concept on shared data.
- **Problem:** Fragments with nothing to hold on to; behaviors on the same data scattered across types.
- **Detection Signals:**
  - Proposals to extract single operations (add/remove/clear/sort-style operations on the same state) into separate classes.
  - Many tiny classes that all manipulate the same data owned elsewhere.
- **Recommended Action:** Keep members that work on the same state for the same concept together, regardless of line count; split only along genuine seams (PCC-227).
- **Exceptions/Trade-offs:** The book's example is `List<T>` — over a thousand lines, one responsibility, highly cohesive.
- **Related Rules:** PCC-227; ch5 SRP; ch11 smaller classes.
- **Review Question:** Would this split separate members that operate on the same data for the same concept?

#### PCC-230
**Give each piece of business knowledge one authoritative representation**
- **Source Chapter:** 13 (section: Applying the DRY principle to knowledge)
- **Principle:** DRY means a business rule, algorithm, or decision is encoded once, not independently in several places that can drift apart. It is broader than "no copy-paste".
- **Problem:** When the policy changes, every copy must be found and updated correctly; missed copies silently diverge.
- **Detection Signals:**
  - The same magic number/policy value in several methods (e.g. a 30-day window written as a literal twice).
  - The same domain condition repeated in several places (e.g. weekend = Saturday or Sunday written twice).
- **Recommended Action:** Introduce a named constant or a single method that represents the rule; make all usages refer to it.
- **Exceptions/Trade-offs:** Only applies to the *same* knowledge — see PCC-233 for coincidental equality.
- **Related Rules:** PCC-231, PCC-233, PCC-247.
- **Review Question:** If this business rule changed tomorrow, would exactly one place need editing?

#### PCC-231
**Build related computations on the authoritative one, not on parallel formulas**
- **Source Chapter:** 13 (section: Applying the DRY principle to knowledge)
- **Principle:** When two operations express the same rule (compute a deadline; check whether it has passed), derive one from the other instead of re-encoding the rule with a different formula.
- **Problem:** Two formulations of one rule (adding days vs. comparing elapsed days) can disagree at boundaries or after a change.
- **Detection Signals:**
  - Sibling methods that each restate the same rule with different arithmetic.
  - A predicate that recomputes what a neighbouring "get" method already computes.
- **Recommended Action:** Have the check call the computing method (and a small shared helper such as an "is before now" predicate) so only one place knows the rule.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-230.
- **Review Question:** Does each related method reuse the single computation of the rule rather than restating it?

#### PCC-232
**Extract mechanical duplication when doing so improves clarity**
- **Source Chapter:** 13 (section: Applying the DRY principle to knowledge; Table 13.3)
- **Principle:** Repeated implementation code that is not duplicated business knowledge (e.g. the same null-or-empty guard for two IDs) can be extracted into a small helper to cut maintenance work.
- **Problem:** Repeated boilerplate must be changed in several places; message formats drift.
- **Detection Signals:**
  - Identical guard clauses differing only in the checked value and its name.
  - Copy-pasted exception construction with only a property name changed.
- **Recommended Action:** Extract a private helper taking the value and its name (`nameof(...)`), call it for each case.
- **Exceptions/Trade-offs:** Conditional: "when doing so improves clarity" — extraction is a maintenance choice, not a domain-model change; do not extract if it obscures the code.
- **Related Rules:** PCC-230, PCC-233; ch3 Writing better methods.
- **Review Question:** Is this repeated code a mechanism (safe to extract) rather than two separate rules?

#### PCC-233
**Keep coincidentally identical rules separate**
- **Source Chapter:** 13 (section: Knowing when duplication is justified; Table 13.3)
- **Principle:** Code that looks the same today may represent different business rules that will change independently. Do not merge rules whose equality is coincidental (return window and refund window both being 30 days).
- **Problem:** Merging them (e.g. making refund methods call return methods) couples unrelated business areas; when one changes (100 days, customer-status dependent), the code must be untangled first — annoying in small code, very hard in real applications.
- **Detection Signals:**
  - A method for concept B implemented by delegating to the method of unrelated concept A because the numbers match.
  - One constant reused for two different policies.
  - "DRY" refactors that unify code across different business areas/owners.
- **Recommended Action:** Keep separate named constants and methods per rule, even with equal values; share only genuinely generic mechanics (e.g. a date-comparison helper).
- **Exceptions/Trade-offs:** Accepts a little textual duplication in exchange for keeping business-separate concepts separate. Merge only if they are truly the same rule.
- **Related Rules:** PCC-230, PCC-234; ch5 SRP (reasons to change).
- **Review Question:** Would these two pieces of code always have to change together for a business reason, or do they just happen to match now?

#### PCC-234
**Tolerate temporary duplication until the shared abstraction reveals itself**
- **Source Chapter:** 13 (section: Avoiding premature abstraction)
- **Principle:** When requirements are uncertain, duplication is safer than an abstraction built on a guessed similarity. Let requirements mature and refactor once the truly shared parts are known.
- **Problem:** Premature abstractions (interfaces/base classes designed for anticipated cases) end up serving problems that never materialize, shaped around wrong assumptions.
- **Detection Signals:**
  - New base classes/interfaces created to unify two pieces of code that have existed only briefly.
  - Abstractions with hooks or parameters for cases no current code uses.
- **Recommended Action:** Keep the similar code separate for now; extract when real, stable commonality appears.
- **Exceptions/Trade-offs:** Waiting costs less than unwinding a wrong abstraction. Contrast with PCC-230: duplicated *knowledge* should still be unified.
- **Related Rules:** PCC-233, PCC-235, PCC-236.
- **Review Question:** Is the commonality this abstraction captures proven by current requirements, or assumed?

#### PCC-235
**BDUF: don't commit to detailed abstractions before the problem is understood**
- **Source Chapter:** 13 (section: Avoiding premature abstraction; Table 13.4)
- **Principle:** Big Design Up Front names the trap of designing detailed abstractions before understanding the problem well enough.
- **Problem:** Elaborate structure built on wrong assumptions.
- **Detection Signals:**
  - Layered interfaces/factories/hierarchies introduced before the first concrete use case works.
  - Design driven by imagined future scenarios rather than current requirements.
- **Recommended Action:** Ask "am I solving a current design problem or an imagined future one?"; design for what is understood now.
- **Exceptions/Trade-offs:** BDUF/YAGNI/KISS do not forbid planning or abstraction; they oppose complexity no current requirement supports.
- **Related Rules:** PCC-234, PCC-236, PCC-237, PCC-238.
- **Review Question:** Is this design solving a current problem rather than an imagined future one?

#### PCC-236
**YAGNI: don't build extension points or features before a real requirement needs them**
- **Source Chapter:** 13 (section: Avoiding premature abstraction; Table 13.4)
- **Principle:** Implement extension points and capabilities only when a real requirement calls for them.
- **Problem:** Speculative complexity with maintenance cost and no user.
- **Detection Signals:**
  - Interfaces with a single implementation and no stated variation; plug-in hooks, strategy slots, configuration switches nothing uses.
  - Unused parameters/overloads "for later".
- **Recommended Action:** Ask "what evidence says this capability will actually be used?"; remove or defer what has none.
- **Exceptions/Trade-offs:** An abstraction over a capability the application already needs, whose implementation is undecided, is not a YAGNI violation (PCC-238).
- **Related Rules:** PCC-235, PCC-238, PCC-221; ch6 OCP (book does not discuss the OCP tension explicitly in this chapter — not stated in book).
- **Review Question:** Is there evidence this extension point or feature will be used?

#### PCC-237
**KISS: prefer the simplest design that keeps clarity and required capability**
- **Source Chapter:** 13 (section: Avoiding premature abstraction; Table 13.4)
- **Principle:** Choose the simplest code that clearly says what it does while preserving correctness and needed capability.
- **Problem:** Unneeded complexity obscures intent.
- **Detection Signals:**
  - Indirection layers or generic machinery whose removal would not reduce expressiveness or correctness.
- **Recommended Action:** Ask "can I remove complexity without making the code less expressive or less correct?" and remove it if so.
- **Exceptions/Trade-offs:** KISS does not mean avoiding advanced language features because a junior might not know them; using such a feature is fine when it expresses intent more directly. The goal is not the lowest-level code.
- **Related Rules:** PCC-235, PCC-236.
- **Review Question:** Can any complexity here be removed without losing clarity or correctness?

#### PCC-238
**Put a needed-but-undecided infrastructure capability behind an interface**
- **Source Chapter:** 13 (section: Deferring design decisions behind abstractions)
- **Principle:** When the application already needs a capability (data access) but the implementation choice (own servers vs. cloud) is undecided, define the interface for what the app needs and work against a simple test implementation until the decision is made.
- **Problem:** Without it, the rest of the application waits on the infrastructure decision, or gets rewritten when it arrives.
- **Detection Signals:**
  - Business code blocked on, or hard-wired to, an undecided storage/transport choice.
  - Absence of a seam where the real implementation could later be plugged in.
- **Recommended Action:** Define a minimal interface from the consumer's needs; provide a trivial in-memory/test implementation; write the real one later without touching consumers.
- **Exceptions/Trade-offs:** Justified only because the capability is needed now; differs from inventing extension points for hypothetical capabilities (PCC-236).
- **Related Rules:** PCC-236, PCC-221; ch9 DIP.
- **Review Question:** Is this abstraction covering a capability the application needs today (only the implementation undecided), rather than a hypothetical one?

#### PCC-239
**Prefer composition over inheritance for sharing and varying behavior**
- **Source Chapter:** 13 (sections: Composition and inheritance; Composition over inheritance)
- **Principle:** To share behavior and vary one part, write one class for the shared part and give it a collaborator (via an interface) for the varying part, rather than an abstract base with overriding subclasses.
- **Problem:** Inheritance brings tight base–derived coupling, trapped behavior, compile-time rigidity, single-base limits, over-exposure, combinatorial hierarchies, awkward tests and ORM mapping (PCC-240–PCC-242).
- **Detection Signals:**
  - Abstract base class whose only variation is one abstract/virtual method overridden in each subclass.
  - Subclasses named `<Variant><BaseName>` that differ in one method.
  - Runtime selection implemented by choosing which subclass to `new`.
- **Recommended Action:** Extract the varying method into an interface; inject an implementation through the constructor and hold it in a readonly field; delete the subclasses in favour of interface implementations; choose the implementation at runtime/config.
- **Exceptions/Trade-offs:** See PCC-243 for cases where inheritance remains appropriate. When inheritance is tempting, ask whether composition would serve better — "most of the time it will".
- **Related Rules:** PCC-240, PCC-241, PCC-242, PCC-243, PCC-244; ch9 DIP; ch7 LSP.
- **Review Question:** Could this inheritance be replaced by injecting a collaborator for the part that varies?

#### PCC-240
**Recognize the hidden costs of inheritance used for reuse**
- **Source Chapter:** 13 (section: Issues of inheritance)
- **Principle:** Inheritance used to share behavior couples base and derived classes, locks reusable logic inside a hierarchy, fixes relationships at compile time, consumes the single base-class slot, and exposes base implementation details to all subclasses.
- **Problem:**
  - Base-class changes ripple (e.g. a new base constructor must be repeated in every subclass).
  - Logic in an override (e.g. data reading) cannot be reused without dragging in the unrelated base (a formatter).
  - The relationship cannot change without editing type declarations.
  - Subclasses inherit more than they need.
  - ORM tools (e.g. Entity Framework) map hierarchies onto flat tables awkwardly, producing complex schemas.
- **Detection Signals:**
  - A change to a base constructor/signature forcing edits in all subclasses.
  - Code instantiating a subclass of X only to call a method unrelated to X's purpose.
  - Subclasses using few of the protected members they inherit.
  - Entity hierarchies mapped by an ORM.
- **Recommended Action:** Move the reusable behavior into its own type behind an interface and compose (PCC-239).
- **Exceptions/Trade-offs:** See PCC-243.
- **Related Rules:** PCC-239, PCC-241, PCC-242, PCC-243.
- **Review Question:** Is any behavior locked inside an inheritance hierarchy that other code would want to reuse independently?

#### PCC-241
**Model each independent dimension of variation as its own collaborator, not a subclass matrix**
- **Source Chapter:** 13 (sections: Issues of inheritance; Composition over inheritance — "Growing hierarchy")
- **Principle:** When types vary along several independent axes (source × format), inheritance multiplies classes per combination; composition adds one interface per axis and stays one level deep.
- **Problem:** Each new varying aspect multiplies the hierarchy until it is unmanageable.
- **Detection Signals:**
  - Class names concatenating two or more variant words (`<Source><Concept><Format>…`).
  - Number of subclasses growing as the product of options.
- **Recommended Action:** One interface per varying aspect with its implementations; the host class receives one collaborator per aspect; structure stays flat.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-239, PCC-240; ch6 OCP.
- **Review Question:** Does adding a new option require a new class per existing combination?

#### PCC-242
**Make shared logic testable by composition, not via abstract bases or test-only subclasses**
- **Source Chapter:** 13 (sections: Issues of inheritance — unit testing; Composition over inheritance)
- **Principle:** Abstract bases cannot be instantiated, so testing them means either duplicating base-logic coverage across every subclass fixture or creating a test-only subclass that does not exist in production. Composition lets the host be tested with a mocked collaborator and each implementation be tested alone.
- **Problem:** Duplicated tests, multiple fixtures to update per base change, and tests that prove less than they appear to.
- **Detection Signals:**
  - Test projects defining concrete subclasses of production abstract classes solely for testing.
  - The same base-class assertions repeated in each subclass's fixture.
- **Recommended Action:** Refactor to composition (PCC-239); test the host with a test double of the interface and each implementation in its own fixture, so one change affects one fixture.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-239, PCC-223; ch15 testable code.
- **Review Question:** Can the shared logic and each variant be tested independently without a test-only subclass?

#### PCC-243
**Keep inheritance where it is the right tool**
- **Source Chapter:** 13 (section: Composition over inheritance)
- **Principle:** Inheritance is not banned. It fits when a framework requires deriving from its base class, for custom exception types (the hierarchy lets callers catch at the right level), and for small, data-focused families whose variants are known upfront and unlikely to grow. It is weak as a general behavior-sharing tool.
- **Problem:** Mechanically replacing all inheritance would fight frameworks and exception handling.
- **Detection Signals:** (for *not* flagging) derivation from framework base types; `: Exception`; small sealed sets of data records.
  - **[add-in illustration]** Nice3point `ExternalCommand`/`ExternalApplication`, WPF `Window`/`UserControl`, `ObservableObject` — framework-required derivation.
- **Recommended Action:** Accept inheritance in these cases; still ask whether behavior sharing beyond the framework contract would be better composed.
- **Exceptions/Trade-offs:** This rule *is* the exception list for PCC-239–PCC-242.
- **Related Rules:** PCC-239, PCC-240; ch7 LSP.
- **Review Question:** Is this inheritance required by a framework, an exception type, or a small closed data family — and if not, why not composition?

#### PCC-244
**Inject collaborators through the constructor instead of creating concrete ones inside methods**
- **Source Chapter:** 13 (section: Refactoring case study — Understanding the code; Improving the code)
- **Principle:** A class that `new`s a concrete collaborator inside a method depends on that concrete type (a DIP violation) and cannot be retargeted without editing it. Depend on an abstraction and receive the implementation via the constructor.
- **Problem:** Example: a vacation planner that creates a Polish holiday provider internally cannot plan for any other country, and cannot swap the provider in tests.
- **Detection Signals:**
  - `new ConcreteService()` / `new …Provider()` inside business methods.
  - Fields assigned from `new` of a concrete service type rather than from a constructor parameter.
- **Recommended Action:** Introduce an interface; add a constructor parameter typed as that interface; store it in a field; compose concrete implementations at the application entry point.
- **Exceptions/Trade-offs:** (not stated in book) — data objects (the book's `VacationPlan` result) are created with `new` in the same example without objection.
- **Related Rules:** PCC-221, PCC-245, PCC-223; ch9 DIP; ch10 Static methods and dependencies.
- **Review Question:** Does this class create any service collaborator itself instead of receiving it?

#### PCC-245
**Hide static infrastructure calls behind an interface**
- **Source Chapter:** 13 (section: Refactoring case study — Identifying the issues; Improving the code)
- **Principle:** Calling a static API (e.g. `Console.WriteLine`) binds the class to it — a hidden coupling and DIP violation. Wrap it in a small interface with a thin implementation and inject it.
- **Problem:** Switching the output channel (GUI, test double) means rewriting all presentation code.
- **Detection Signals:**
  - Direct `Console.*`, other static I/O or UI calls inside classes that should not know the channel.
  - **[add-in illustration]** `TaskDialog.Show(...)` / `MessageBox.Show(...)` scattered through services.
- **Recommended Action:** Define an interface for the needed operation (show a message), implement it once over the static API, inject it; consumers no longer know the static API exists.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-244, PCC-246; ch10 Static methods and dependencies; ch9 DIP.
- **Review Question:** Is there a static infrastructure call in this class that should be an injected dependency?

#### PCC-246
**Separate presenting results from computing them**
- **Source Chapter:** 13 (section: Refactoring case study — Identifying the issues; Improving the code)
- **Principle:** A class responsible for producing a result (a vacation plan) should not also print a report of it; move reporting into a dedicated class (a printer) that depends on an output abstraction.
- **Problem:** SRP violation and coupling to the output channel inside domain logic.
- **Detection Signals:**
  - Public `Show…/Print…/Report…` methods on domain/calculation classes.
  - Calculation classes formatting strings for the user.
- **Recommended Action:** Extract a presentation class taking the result as input and an output interface as dependency; the entry point wires planner and printer.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-227, PCC-245; ch5 SRP.
- **Review Question:** Does this computing class also format or display its results?

#### PCC-247
**Place knowledge in the class that naturally owns it**
- **Source Chapter:** 13 (section: Refactoring case study — Improving the code)
- **Principle:** Knowledge should live where it belongs conceptually: the type that knows a country's public holidays should also know which days are the weekend there, so the weekend check moves from the planner into the holiday-provider abstraction.
- **Problem:** The consumer holds domain knowledge that varies with its collaborator, so a different variant (Friday–Saturday weekend) needs edits in the consumer.
- **Detection Signals:**
  - A consumer computing facts that depend on the same variation axis its injected collaborator represents.
  - Helper predicates in a class that are "not its job".
- **Recommended Action:** First consolidate duplicated logic into one method (PCC-230), then move that method to the owning type and add it to its interface.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-230, PCC-227, PCC-221; ch5 SRP.
- **Review Question:** Does each piece of domain knowledge sit in the type that represents that domain concept?

#### PCC-248
**Make contract return types general so implementations choose the concrete collection**
- **Source Chapter:** 13 (section: Refactoring case study — Improving the code)
- **Principle:** Interface members should return a general type (`IEnumerable<T>` rather than `List<T>`) so each implementation decides what collection to use.
- **Problem:** A concrete collection in a contract forces every implementation's internal choice.
- **Detection Signals:**
  - Interface members returning `List<T>`, `T[]`, `Dictionary<,>` where consumers only iterate.
- **Recommended Action:** Change the declared return type to the most general type consumers need.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-219; ch8 ISP.
- **Review Question:** Does this contract force implementers into a specific collection type the consumer does not need?

#### PCC-249
**Use one term per concept, and never a term already meaning something else in the code**
- **Source Chapter:** 13 (section: Refactoring case study — Identifying the issues; Improving the code)
- **Principle:** Pick one word for a concept and use it consistently; prefer the word that does not collide with another concept in the same code (vacation vs. holiday, where holiday already means public holiday).
- **Problem:** Interchangeable terms confuse readers about whether two things are the same.
- **Detection Signals:**
  - Synonyms for one concept across identifiers (`…HolidayLength` and `…VacationLength` for the same thing).
  - A word used both for a domain concept and for something else in the same module.
- **Recommended Action:** Choose the term, rename identifiers (IDE rename) consistently.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** ch2 Meaningful names.
- **Review Question:** Is each concept named with exactly one term throughout this code?

#### PCC-250
**Do the mechanical hygiene first: format, one type per file, folders, public before private**
- **Source Chapter:** 13 (section: Refactoring case study — Identifying the issues; Improving the code)
- **Principle:** Start a refactoring with IDE formatting (break overlong lines, spacing between methods); put classes in separate files within a sensible folder structure; order members so public ones are not buried below private ones; align parallel conditions vertically so comparisons are easy to see.
- **Problem:** All classes in one file, overlong lines, and public methods hidden at the bottom make structure hard to read.
- **Detection Signals:**
  - Multiple top-level types in one `.cs` file.
  - Lines too long for the screen; inconsistent blank lines between methods.
  - Public methods placed after private helpers.
  - Compound boolean conditions on one line where vertical alignment would show the parallel structure.
- **Recommended Action:** Reformat with the IDE, split files, organize folders, reorder members, align related conditions.
- **Exceptions/Trade-offs:** The book's folder structure figure (Figure 13.1) is an image not present in the text; exact layout not stated.
- **Related Rules:** ch4 Formatting code; ch12 Organizing classes and projects.
- **Review Question:** Is the code formatted, one type per file, and ordered with public members first?

### Code smells & anti-patterns catalogue

| Name | Signal | Rule IDs |
|---|---|---|
| Leaky collection property | Public `T[]`/`List<T>` returning the backing field; consumers index or use `.Length` | PCC-219, PCC-248 |
| Foreign mutation | `other.Items[i] = …` / `.Add()` on another object's collection | PCC-220 |
| Concrete dependency | Fields/params typed as concrete services where variants exist | PCC-221, PCC-244 |
| Change cascade | One internal change edits several unrelated files | PCC-222 |
| Heavy test setup | Many collaborators built to test one class | PCC-223, PCC-242 |
| Type-switching client | `is`/`as`/`GetType()` on implementations of an abstraction | PCC-224 |
| Train wreck / chain navigation | `a.B().C().D` through other objects' internals | PCC-225 |
| Context dumpster parameter | Whole context object passed to extract one value | PCC-226 |
| Low-cohesion class | Member groups using disjoint fields/dependencies | PCC-227, PCC-228 |
| Compound / vague class name | Name cannot describe the whole type | PCC-228 |
| Over-split class | One operation per class over shared data | PCC-229 |
| Duplicated business knowledge | Same policy literal/condition in several places | PCC-230, PCC-247 |
| Parallel formulas | Sibling methods restating a rule with different arithmetic | PCC-231 |
| Copy-paste guards | Identical validation blocks differing by name | PCC-232 |
| Coincidental-duplication merge | Concept B delegating to concept A because values match | PCC-233 |
| Premature abstraction / BDUF | Hierarchies or hooks for unproven cases | PCC-234, PCC-235, PCC-236 |
| Gratuitous complexity | Indirection removable without loss | PCC-237 |
| Inheritance for reuse | Abstract base with one overridden method per subclass | PCC-239, PCC-240 |
| Combinatorial hierarchy | Subclasses named by concatenated variants | PCC-241 |
| Test-only subclass | Test project subclassing production abstract class | PCC-242 |
| `new` inside method | Business method instantiating a concrete service | PCC-244 |
| Hidden static coupling | `Console.*`/static UI calls inside logic | PCC-245 |
| Report-in-calculator | Domain class with `Show/Print/Report` methods | PCC-246 |
| Misplaced knowledge | Consumer computing facts its collaborator should own | PCC-247 |
| Synonym drift | Two words for one concept, or one word for two | PCC-249 |
| Multi-type file / buried public API | Several classes per file; public below private | PCC-250 |

### Refactoring techniques named in the chapter
- Replace exposed concrete collection with `IEnumerable<T>` view over a private field.
- Pass the needed value directly instead of a context object (Law of Demeter).
- Split a low-cohesion class along the seam where member groups meet (pricer → pricer + notifier).
- Introduce a named constant for a business rule; make related checks call the authoritative computation.
- Extract a small validation helper (value + `nameof`) for mechanical duplication.
- Keep separate constants/methods for coincidentally equal rules; share only a generic helper.
- Defer abstraction until commonality is proven; refactor once requirements mature.
- Put an undecided infrastructure capability behind an interface with a simple test implementation.
- Replace inheritance with composition: abstract method → interface; inject via constructor into a readonly field; subclasses → implementations; choose at runtime.
- One interface per varying aspect instead of a subclass per combination.
- Replace `new ConcreteX()` inside a method with a constructor-injected abstraction (DIP).
- Wrap a static API (Console) behind a one-method interface with a thin implementation.
- Extract presentation into a dedicated printer class that depends on the output interface.
- Consolidate a duplicated condition into one method, then move it to the class that owns that knowledge and add it to its interface.
- Generalize contract return types (`List` → `IEnumerable`).
- Rename for one-term-per-concept consistency.
- IDE reformat, split long lines, one type per file, folder structure, public-before-private ordering, vertical alignment of parallel conditions.

### Things the author says NOT to do mechanically
- Do not treat every dependency as a defect — collaboration is necessary; the issue is excess knowledge and needless concreteness.
- Do not introduce interfaces unconditionally — the table ties abstraction to *expected variation*.
- Do not split every method into its own class to satisfy a literal SRP; size alone is not low cohesion (`List<T>`).
- Do not read DRY as "never repeat a line"; it is about knowledge.
- Do not merge code that only looks alike — coincidentally equal rules (return vs. refund window) stay separate.
- Do not remove every visual similarity; that can be as harmful as indiscriminate copy-paste.
- Extract mechanical duplication only when it improves clarity.
- Do not build abstractions for anticipated cases before requirements settle; temporary duplication can be safer.
- BDUF/YAGNI/KISS do not prohibit planning or abstraction — only complexity no current requirement supports.
- KISS does not mean avoiding advanced language features.
- An interface over a needed capability with an undecided implementation is not speculative.
- Do not ban inheritance — frameworks, custom exceptions, and small closed data families are legitimate uses.
- Law of Demeter on data structures / fluent APIs / LINQ: no explicit statement in the book (not stated in book); the book's own improved code chains calls on a `DateTime` value and uses LINQ pipelines without comment, so do not flag every dot-chain.

---

## Chapter 14 — Using comments effectively

### Chapter summary
- More comments is not better: a comment can add what code cannot say, but it is also a second description of the program that agrees with the code only until one of them changes.
- Obvious comments add noise; outdated comments are worse because they assert something false. Tools can rename symbols but cannot detect prose that became untrue.
- Let code carry as much intent as possible (names, smaller methods, clearer types, structure); comments are for what genuinely belongs outside the implementation. "Don't comment bad code, rewrite it" (Kernighan & Plauger).
- Harmful patterns: restating comments, navigation markers, block-description comments that should be methods, changelog headers, commented-out code — version control and refactoring handle these better.
- Legitimate comments: references for complex algorithms, explanation + examples for regexes, reasons for temporary workarounds, small consistent TO-DOs, structured XML docs for shared/public APIs, and concise mandatory copyright notices.
- Gate for every comment: add it only when it contributes information that code, naming, structure, tooling, or VCS history cannot carry more reliably; when needed, make it precise, complete, brief, and maintainable.

### Rules

#### PCC-251
**Fix the name or the structure instead of explaining it in a comment**
- **Source Chapter:** 14 — Using comments effectively (section: Why comments should not replace clear code)
- **Principle:** If a variable or method needs a comment to say what it means, rename it; if a method needs a running commentary, refactor it first. Comments must not be a translation layer for poor names or tangled control flow.
- **Problem:** The comment is a second artifact maintained separately from the code and can drift; the unclear code remains.
- **Detection Signals:**
  - Comments defining what a cryptic identifier means (`// x = number of retries`).
  - Long methods interleaved with step-by-step explanatory comments.
  - New comments added in a diff next to confusing code rather than changes to the code.
- **Recommended Action:** Rename to an intention-revealing identifier; extract methods; introduce clearer types; only then decide whether any comment is still needed.
- **Exceptions/Trade-offs:** Comments that carry context code cannot express are fine (PCC-258–PCC-264).
- **Related Rules:** PCC-255, PCC-258; ch2 Meaningful names; ch3 Writing better methods.
- **Review Question:** Would a better name, smaller method, or clearer type make this comment unnecessary?

#### PCC-252
**Treat code as the source of truth; keep comments only where their value pays for their upkeep**
- **Source Chapter:** 14 (sections: Why comments should not replace clear code; Outdated and misleading comments)
- **Principle:** Comment count is not a quality metric. Every comment must be maintained alongside the code; an IDE cannot tell that prose has become false after a behavior change. A comment that contradicts the code misleads maintainers.
- **Problem:** Outdated comments state things that are no longer true and send readers the wrong way.
- **Detection Signals:**
  - A diff that changes behavior while leaving an adjacent comment describing the old behavior.
  - Comments mentioning parameters, values, units, or branches that no longer exist.
  - Team rules or reviews that demand comments on every member for their own sake.
- **Recommended Action:** On every behavior change, update or delete nearby comments; prefer moving the information into names/structure so it cannot drift.
- **Exceptions/Trade-offs:** The "safest" comment explains information beyond the mechanics of the implementation (it is less likely to be invalidated by refactoring).
- **Related Rules:** PCC-251, PCC-253, PCC-258.
- **Review Question:** Does every comment touched by (or adjacent to) this change still describe the code accurately?

#### PCC-253
**Delete "Captain Obvious" comments that restate the code**
- **Source Chapter:** 14 (section: Removing comments that restate the code)
- **Principle:** A comment that repeats what identifiers already say (that a constructor is a constructor, that a file-name variable holds a file name, that a distance-in-kilometers method computes distance in kilometers) adds no information.
- **Problem:** Noise that dilutes useful comments and can still go stale.
- **Detection Signals:**
  - `// constructor`, `// getter`, `// save file name` above the obviously named member.
  - Comment text that is a reworded copy of the method/variable name.
- **Recommended Action:** Remove them; let identifiers speak.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-252; ch2 Meaningful names.
- **Review Question:** Does this comment say anything the code's names do not already say?

#### PCC-254
**Replace navigation comments with formatting and extraction**
- **Source Chapter:** 14 (section: Removing navigation comments)
- **Principle:** Markers such as "end of for loop" indicate that formatting or method size hides the structure.
- **Problem:** The marker treats the symptom (lost structure) instead of the cause.
- **Detection Signals:**
  - `} // end of for`, `} // end if`, `// end class`, banner/region dividers used to find your place in long bodies.
- **Recommended Action:** Format consistently and extract oversized blocks into methods so boundaries are visible.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-255; ch3 Writing better methods; ch4 Formatting code.
- **Review Question:** Is a comment here only to help the reader find the end of a block?

#### PCC-255
**Turn block-description comments into well-named methods**
- **Source Chapter:** 14 (section: Replacing block-description comments with methods)
- **Principle:** A comment naming what a block does ("read the IDs from the TXT file") marks a responsibility that should be its own method; the developer noticed the seam but stopped short of acting.
- **Problem:** A comment stays only where it was written; a method name travels to every call site and documents each use, so names are more reliable documentation.
- **Detection Signals:**
  - A comment line followed by a cohesive block, repeated for several branches (`// read X`, `// read Y`).
  - Duplicate preparatory lines inside each commented branch.
- **Recommended Action:** Extract each block into a method named after the comment; hoist shared preparation (e.g. reading file content once) out of the branches; delete the comments.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-251, PCC-254; ch3 Writing better methods.
- **Review Question:** Could the text of this comment become the name of an extracted method?

#### PCC-256
**Keep change history in version control, not in file-header changelogs**
- **Source Chapter:** 14 (section: Keeping change history in version control)
- **Principle:** Dates, authors, renames, moves, and cleanups belong in repository history and commit messages, which track them more accurately.
- **Problem:** Header changelogs duplicate VCS data less reliably and clutter the file.
- **Detection Signals:**
  - Top-of-file comment lists of `date – author – change` entries; "renamed to…", "moved to…", "cleanup" notes.
- **Recommended Action:** Delete the changelog; write meaningful commit messages instead.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-257.
- **Review Question:** Does this file carry history that version control already records?

#### PCC-257
**Delete commented-out code**
- **Source Chapter:** 14 (section: Removing commented-out code)
- **Principle:** Readers cannot tell whether commented-out code is obsolete, temporarily disabled, or still needed. Remove unused code; version control can recover it.
- **Problem:** Uncertainty and clutter that nobody dares to remove.
- **Detection Signals:**
  - Lines of `//`-prefixed C# statements, or `/* … */` blocks containing code.
  - Old implementations kept "just in case" beside new ones.
- **Recommended Action:** Delete; rely on VCS history for recovery.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-256.
- **Review Question:** Is there any commented-out code in this change?

#### PCC-258
**Write a comment only for context the code cannot carry, and make it precise, complete, brief, and maintainable**
- **Source Chapter:** 14 (sections: When comments add value; Keeping required copyright comments concise — closing rule; Summary)
- **Principle:** A comment is justified when it carries context that names, structure, tooling, or VCS cannot express more reliably — the reason for a workaround, which algorithm is implemented, what a regex matches. Before writing it, check whether a better name, smaller method, or clearer type would say the same.
- **Problem:** Without this gate, comments either repeat the code or replace fixable code.
- **Detection Signals:**
  - Non-obvious decisions (workarounds, chosen algorithms, magic patterns) with no explanation.
  - Comments that are long, vague, or incomplete.
- **Recommended Action:** Prefer code that explains its own mechanics; add a short, precise comment for the remaining *context/why*.
- **Exceptions/Trade-offs:** This is the umbrella rule; PCC-259–PCC-264 are its sanctioned cases.
- **Related Rules:** PCC-251, PCC-259–PCC-264.
- **Review Question:** Does this comment add information that code, naming, structure, tooling, or version control could not carry more reliably?

#### PCC-259
**Name and reference complex algorithms**
- **Source Chapter:** 14 (section: Documenting complex algorithms)
- **Principle:** For a well-known but complex algorithm (adaptive heap sort), good names may not suffice for an unfamiliar reader; a short comment naming the algorithm with a link to a description lets readers find out what is implemented.
- **Problem:** Readers waste time reverse-engineering a known algorithm.
- **Detection Signals:**
  - Non-trivial numeric/geometric/sorting algorithm with no indication of what it is or where it comes from.
- **Recommended Action:** Add a brief comment stating the algorithm and a reference link; brief step comments may mark key phases.
- **Exceptions/Trade-offs:** This does not excuse unclean code — the code must still be as clean as possible.
- **Related Rules:** PCC-258, PCC-251.
- **Review Question:** If this implements a known algorithm, does a comment name it and point to a description?

#### PCC-260
**Explain every non-trivial regular expression with intent and examples**
- **Source Chapter:** 14 (section: Explaining regular expressions)
- **Principle:** Regexes are hard to read; do not make readers decipher them. Comment what the pattern does and give examples of matching and non-matching strings.
- **Problem:** Unreadable patterns are hard to verify or change safely.
- **Detection Signals:**
  - `Regex`, `Regex.IsMatch`, `new Regex(...)`, `[GeneratedRegex]` or pattern strings with no explanatory comment.
  - Pattern comments without example inputs.
- **Recommended Action:** Add a comment describing what is matched plus a few matching and non-matching examples.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-258.
- **Review Question:** Does each regex have a comment explaining what it matches with positive and negative examples?

#### PCC-261
**Document temporary workarounds: why, that they are temporary, when, and who to ask**
- **Source Chapter:** 14 (section: Documenting temporary workarounds)
- **Principle:** When code must contain a workaround that cannot be fixed now (e.g. a library that fails on first construction and works on the second), leave a comment explaining the reason, marking it as temporary and to be addressed, with a date and a contact.
- **Problem:** Without the comment, the odd code looks like a mistake and may be "fixed" or never revisited.
- **Detection Signals:**
  - Retry-twice, swallowed-exception, or sleep/hack code with no explanation.
  - Empty `catch` blocks with no stated reason.
- **Recommended Action:** Add a comment covering the cause, that it is temporary, the date, and a person to contact.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-258, PCC-262.
- **Review Question:** Does every workaround say why it exists, that it is temporary, when it was added, and whom to ask?

#### PCC-262
**Use TO-DO comments only for small, short-lived tasks, in one consistent searchable format**
- **Source Chapter:** 14 (section: Using TO-DO comments carefully)
- **Principle:** Larger work items belong in an issue tracker. TO-DOs in code are for small, short-lived tasks, in a consistent format, with owner and date when that helps the team, and tracked by analyzers or repository search so they do not linger forever.
- **Problem:** Inconsistent spellings defeat automated search; large or ownerless TO-DOs never get done.
- **Detection Signals:**
  - Mixed variants (`TODO`, `TO DO:`, `To-do`, `to-do`) in the codebase.
  - TO-DOs describing large features or redesigns.
  - Old TO-DOs with no owner/date.
- **Recommended Action:** Normalize to the team's single format; add owner/date where useful; move large items to the tracker; periodically search for and resolve TO-DOs.
- **Exceptions/Trade-offs:** Owner/date is "when that helps the team", not mandatory.
- **Related Rules:** PCC-261, PCC-256.
- **Review Question:** Is this TO-DO small, short-lived, in the standard format, and traceable?

#### PCC-263
**Use XML documentation comments for shared/public APIs, describing the contract**
- **Source Chapter:** 14 (section: Using structured documentation comments)
- **Principle:** Structured docs (`/// <summary>`, `<param>`, `<returns>`, `<exception>`) reach callers in the IDE tooltip when they need them. They must be kept in step with the API, which is worth it for shared libraries and public interfaces. They should describe the contract, not narrate the implementation.
- **Problem:** Undocumented shared APIs force callers to read source; docs on project-internal code rarely repay their upkeep; implementation-narrating docs go stale.
- **Detection Signals:**
  - Public members of shared libraries/public interfaces without `///` docs, or missing thrown-exception docs.
  - `///` blocks on internal/private code that merely restate names.
  - `<summary>` text describing internal steps instead of behavior, inputs, outputs, exceptions.
  - **[add-in illustration]** contracts in a shared library consumed by several hosts vs. private helpers inside one feature folder.
- **Recommended Action:** Document the contract (purpose, parameters, return, exceptions) on shared/public APIs; skip or remove for code used only within one project unless the audience justifies it.
- **Exceptions/Trade-offs:** For code used only inside one project, the maintenance cost is rarely repaid.
- **Related Rules:** PCC-252, PCC-258.
- **Review Question:** Is this API shared or public enough to justify XML docs, and do they describe the contract rather than the implementation?

#### PCC-264
**Keep required copyright/licence headers as short as policy allows**
- **Source Chapter:** 14 (section: Keeping required copyright comments concise)
- **Principle:** When an organization requires copyright or licence notices in source files, keep them minimal and prefer a link to the authoritative legal text.
- **Problem:** Long legal boilerplate clutters every file.
- **Detection Signals:**
  - Multi-paragraph licence text pasted at the top of each file.
- **Recommended Action:** Shorten to what policy requires; link to the full text.
- **Exceptions/Trade-offs:** Only where a policy requires such notices.
- **Related Rules:** PCC-258.
- **Review Question:** Is the required header as concise as the policy permits?

### Code smells & anti-patterns catalogue

| Name | Signal | Rule IDs |
|---|---|---|
| Comment as translation layer | Comment explaining a cryptic name or tangled flow | PCC-251 |
| Stale / misleading comment | Comment disagrees with code after a behavior change | PCC-252 |
| Comment-count metric | Comments required on everything for their own sake | PCC-252, PCC-263 |
| Captain Obvious | Comment rewording the member name | PCC-253 |
| Navigation marker | `// end of for`, `// end if` | PCC-254 |
| Block-description comment | `// read X` above a cohesive block | PCC-255 |
| Changelog header | Date/author/change list at file top | PCC-256 |
| Commented-out code | `//` code lines or `/* */` code blocks | PCC-257 |
| Unexplained algorithm | Complex known algorithm with no name/reference | PCC-259 |
| Bare regex | Pattern without intent and examples | PCC-260 |
| Silent workaround | Hack/empty catch with no reason/date/contact | PCC-261 |
| TO-DO graveyard | Inconsistent, large, ownerless, old TO-DOs | PCC-262 |
| Undocumented public API / over-documented internals | Missing `///` on shared API; restating `///` on internal code | PCC-263 |
| Legal wall | Long licence text per file | PCC-264 |

### Refactoring techniques named in the chapter
- Rename variables/methods instead of commenting their meaning.
- Refactor/extract methods before adding prose to a complex method.
- Reformat and extract oversized blocks instead of navigation comments.
- Extract commented blocks into methods named after the comment; hoist shared preparation (read file content once).
- Move change history into commits; delete header changelogs.
- Delete commented-out code; recover from VCS if ever needed.
- Comment complex algorithms with name + reference link; regexes with intent + matching/non-matching examples; workarounds with reason, temporary status, date, contact.
- Normalize TO-DO format; route large items to an issue tracker; use analyzers/search to keep TO-DOs from lingering.
- XML documentation comments describing the contract for shared/public APIs.
- Shorten mandated copyright headers; link to legal text.

### Things the author says NOT to do mechanically
- Do not maximize comments — count is not quality.
- Do not strip every comment either — algorithms, regexes, workarounds, small TO-DOs, public API docs, and required copyright notices legitimately need them.
- An algorithm comment does not excuse leaving the code itself unclean.
- Do not put XML docs on everything — for code used only inside one project the cost is rarely repaid.
- Do not use TO-DOs for large work items; and owner/date are added "when that helps the team", not by rote.
- Do not narrate the implementation in API docs — document the contract.

---

## Chapter 15 — Writing testable code and clean tests

### Chapter summary

- Code can look clean and still be risky to change. Manual re-checking does not scale, so testability has to be **designed in**. Automated tests are the safety net, mocks remove real infrastructure, and dependency injection creates the seams that make both possible.
- A good unit test is **automated, focused, fast, isolated and repeatable** (Table 15.1). Each test arranges data, runs one behavior and checks the result. The author uses NUnit and Moq but says the structure is the same in every framework.
- Unit tests give fast feedback on small behaviors and **document** the class in a way that cannot silently go stale. They **do not prove the application works**, because collaboration is the job of integration and end-to-end tests. Test-hygiene rules apply to every kind of test.
- Tests do not make code clean by themselves. They make **small-step refactoring safe**. When a test is **hard to write**, that is early design feedback: dependencies are hidden, concrete or too tightly coupled, and seams are missing. TDD gets this feedback from the first line by writing the caller-view test first.
- **Test code is production code.** Use descriptive and consistent names (method / expected result / scenario; underscores and long names are fine) and short tests with shared setup where appropriate. Keep arrange/act/assert visible, test one behavior per test, keep logic out of tests, parameterize instead of copy-pasting, and assert only what the behavior promises.
- **Testability is fixed in the production code, not in the test.** Four pitfalls (Table 15.3):
  - static calls to things you would want to control;
  - concrete dependencies;
  - dependencies created internally;
  - constructors that do real work, which hide I/O, cost and side effects, and fire unseen through `Lazy<T>` or the base constructor of a mocked subclass.
- **Sorter case study:**
  - production code: remove comments that restate the code, change-history comments, loop-end markers and commented-out code, and rename `n`;
  - tests: split an overgrown three-case test, drop a redundant case, parameterize the rest, unify names, drop noisy AAA comments;
  - add the two missing behavior tests (null input throws; input list is not modified).
- **Terminology note:** the chapter uses "mock" for any test double. It does not distinguish mocks, stubs, fakes or spies, and it does not discuss DI containers. See the coverage gaps at the end.

### Rules

> Notation: lines tagged **[Revit review hint — not from book]** apply the author's rule to this repo's stack. They are not claims made by the book. Framework names other than NUnit/Moq are equivalents added for convenience.

#### PCC-265
**Make every test runnable automatically, the whole suite from one command**
- **Source Chapter:** 15 — Writing testable code and clean tests (section: Understanding unit testing; Table 15.1 "Automated")
- **Principle:** A unit test is an automated check of one small piece of functionality. A framework discovers and runs it with no manual steps, and the entire suite starts from a single button or command.
- **Problem:** Manual re-checking after every refactoring is slow. People start skipping steps, and defects surface only after release.
- **Detection Signals:**
  - "verification" done by launching the app and clicking through, or a checklist document instead of tests;
  - test methods that need hand preparation (seed a DB by hand, copy a file, set an env var first);
  - checks written as console programs or debug-only code paths rather than framework tests (no `[Test]` / `[Fact]` / `[TestCase]`);
  - **[Revit review hint — not from book]** pure-logic checks that can only be done by opening Revit and pressing a ribbon button.
- **Recommended Action:** Move the checks into a test framework (the book uses NUnit) so they run from one command (for example `dotnet test`). Keep the arrange → execute one behavior → verify shape.
- **Exceptions/Trade-offs:** The choice of framework barely matters; switching is mostly learning new names for the same things.
- **Related Rules:** PCC-266, PCC-273, PCC-276.
- **Review Question:** Can every check covering this change run unattended from a single command?

#### PCC-266
**Keep the unit suite fast enough to run after every change**
- **Source Chapter:** 15 — Writing testable code and clean tests (section: Understanding unit testing; Table 15.1 "Fast")
- **Principle:** The suite should finish in seconds for a small project. Speed is not free: tests have to be written and kept fit for it.
- **Problem:** People stop running a slow suite, and it no longer works as continuous feedback. A slow suite also undermines the refactoring safety net (PCC-273).
- **Detection Signals:**
  - unit tests that open real DB connections, files, network or web services;
  - `Thread.Sleep` / `Task.Delay` in unit tests;
  - fixture setup that boots heavy infrastructure;
  - suite runtime measured in minutes;
  - objects with expensive constructors created repeatedly in tests (PCC-289).
- **Recommended Action:**
  - replace slow collaborators with mocks (PCC-269);
  - take hidden work out of constructors (PCC-289);
  - keep slower collaboration checks in integration/E2E suites (PCC-271).
- **Exceptions/Trade-offs:** Integration tests are acknowledged to take longer. The book gives no numeric budget beyond "seconds for a small project".
- **Related Rules:** PCC-269, PCC-271, PCC-289.
- **Review Question:** Does this change keep the unit suite running in seconds, with no real I/O?

#### PCC-267
**Isolate each test so it fails only when its own class changes**
- **Source Chapter:** 15 — Writing testable code and clean tests (section: Understanding unit testing; Table 15.1 "Isolated"; Using mocks for isolated tests)
- **Principle:** The code under test, not external systems, should control a test's result. A test for class B should not go red when only class A changed. Isolation is designed in, mainly by keeping dependencies explicit and substituting them in tests.
- **Problem:**
  - when B's tests break because A changed, the two classes are more entangled than they should be;
  - failures become unpredictable and get caused by unrelated components;
  - a test that also exercises a real repository is no longer a test of the class alone.
- **Detection Signals:**
  - a one-class production change turns tests of other classes red;
  - test arrange builds chains of real collaborators (`new A(new B(new C(...)))` down to infrastructure);
  - expected values that depend on what a database or file happens to contain;
  - tests needing global configuration;
  - **[Revit review hint — not from book]** pure geometry/maths tests that need a live `Document`.
- **Recommended Action:** When cross-class breakage appears, **stop and inspect the design, not the test**. Make dependencies explicit and substitute them (PCC-285–PCC-288).
- **Exceptions/Trade-offs:** Isolation does not mean replacing everything. Collaborators with nothing to substitute, such as pure maths or string trimming, may run for real (PCC-285).
- **Related Rules:** PCC-269, PCC-274, PCC-285–PCC-288; ch10 hidden dependency graphs.
- **Review Question:** Would this test fail only if the class it names changes behavior?

#### PCC-268
**Make tests repeatable and independent of run order**
- **Source Chapter:** 15 — Writing testable code and clean tests (sections: Understanding unit testing, Table 15.1 "Repeatable"; What unit tests do and do not prove; Avoiding static dependencies that must be replaced)
- **Principle:** The same inputs must give the same outcome on every machine and every run, and no test may depend on the order tests run in.
- **Problem:** Tests tied to database contents, the system clock or execution order stop being dependable, and they fail on another machine.
- **Detection Signals:**
  - `DateTime.Now` / `DateTime.Today` / `DateTime.UtcNow`, the file system or environment read directly inside logic under test (no seam);
  - static mutable state shared between tests;
  - tests that pass only alone or only in a given order;
  - machine-specific paths in tests.
- **Recommended Action:** Put the clock, files and storage behind injectable abstractions (PCC-285, PCC-286). Build each test's state fresh, for example in a per-test setup method (PCC-278).
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-285, PCC-286, PCC-272; ch10 global state.
- **Review Question:** Would this test give the same result on another machine, at another time, in any order?

#### PCC-269
**Replace infrastructure collaborators with mocks in unit tests**
- **Source Chapter:** 15 — Writing testable code and clean tests (section: Using mocks for isolated tests)
- **Principle:** A mock stands in for a real dependency, has no logic of its own, and returns whatever the test configures. Mocks are normally generated by a mocking library rather than written by hand.
- **Problem:** Using a real database makes the test slower, harder to run elsewhere, dependent on the stored data, and it tests the repository as well as the class.
- **Detection Signals:**
  - unit tests constructing real repositories, DB contexts, HTTP clients or file readers;
  - test data seeded into a real store for a unit test;
  - a test whose expected value depends on external data.
- **Recommended Action:** Depend on an interface (PCC-286) and inject it (PCC-288). In the test, create a mock (the book uses Moq: configure the method, set its return value, pass the mock object into the constructor), then call the method and assert on the result. Example: a person processor's max-age query tested against a mocked repository returning three people.
- **Exceptions/Trade-offs:**
  - Do not mock deterministic, nothing-to-substitute collaborators (PCC-285).
  - The book gives no stub/fake/spy taxonomy and no explicit over-mocking warning (not stated in book).
- **Related Rules:** PCC-267, PCC-270, PCC-286, PCC-287, PCC-288.
- **Review Question:** Does each unit test replace every slow or external collaborator with a mock it configures itself?

#### PCC-270
**Verify the call on the collaborator when the behavior is an action**
- **Source Chapter:** 15 — Writing testable code and clean tests (sections: Using mocks for isolated tests; Keeping constructors simple)
- **Principle:** When the method under test returns nothing useful and its effect is a call on a collaborator, use the mock to check that the call happened with the expected arguments.
- **Problem:** Not elaborated in the book beyond this: such methods have no return value to assert on. Examples given: a user-communication mock checked for `ShowMessage` with the right text, and a repository mock checked for `Save` with the right house.
- **Detection Signals:**
  - `void` methods whose whole job is to save, notify or show, with no test, or with a test that has no assertion;
  - tests that call such a method and only check that "no exception was thrown".
- **Recommended Action:** Assert on the mock interaction: the expected method was called with the expected argument values.
- **Exceptions/Trade-offs:** Not stated specifically for interaction checks. PCC-284 (assert only what the behavior promises) applies.
- **Related Rules:** PCC-269, PCC-284, PCC-289.
- **Review Question:** For this action-only method, does a test check that the collaborator was called with the right arguments?

#### PCC-271
**Do not read a green unit suite as proof that the application works**
- **Source Chapter:** 15 — Writing testable code and clean tests (section: What unit tests do and do not prove)
- **Principle:** Unit tests isolate classes on purpose, so they say nothing about how classes behave together. That is the job of integration and end-to-end tests. Unit tests give fast feedback on small behaviors and surface design problems when isolation turns out to be hard.
- **Problem:** Wiring and collaboration defects pass unnoticed behind a fully green unit suite.
- **Detection Signals:**
  - release or PR claims of "verified" backed only by mocked unit tests;
  - no integration test anywhere for a new interaction between components;
  - every collaborator mocked in every test, with nothing exercising the real wiring;
  - **[Revit review hint — not from book]** Revit-API-touching code whose only tests are host-free unit tests, while in-host tests are skipped.
- **Recommended Action:** Add integration or end-to-end tests for collaboration paths, and keep the unit suite in its narrow role.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-269, PCC-272.
- **Review Question:** Is there a test beyond unit level that exercises the real collaboration this change introduces?

#### PCC-272
**Hold integration and end-to-end tests to the same hygiene**
- **Source Chapter:** 15 — Writing testable code and clean tests (section: What unit tests do and do not prove)
- **Principle:** A clear name, one reason to fail, no logic in the test body and no dependence on run order are not specific to unit tests. They apply to every kind of test.
- **Problem:** An unreadable integration test is as much of a liability as an unreadable unit test, and probably more, because it usually takes longer to run.
- **Detection Signals:** integration or harness tests with generic names, several scenarios in one test, loops or branches in the body, or state carried over between tests.
- **Recommended Action:** Apply PCC-277–PCC-284 to integration and end-to-end suites as well.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-268, PCC-277, PCC-280, PCC-281.
- **Review Question:** Would this integration test pass the same readability review as a unit test?

#### PCC-273
**Refactor in small steps and run the suite after each one**
- **Source Chapter:** 15 — Writing testable code and clean tests (sections: Using tests to protect and improve code quality; Using tests as a safety net for refactoring; case study end)
- **Principle:** First make the code work, then improve it in small refactoring steps (rename, extract method, move a responsibility), running the tests after each step. Tests make changes safe, but they do not make code clean by themselves.
- **Problem:**
  - without tests, teams avoid touching working code, so it keeps getting worse;
  - large unchecked changes make a breakage hard to locate;
  - a large suite around a bad design leaves the design exactly as bad;
  - messy tests make every change expensive (Figure 15.1 feedback loop: clean tests → safe refactoring → clean code).
- **Detection Signals:**
  - big refactoring diffs on classes with no covering tests;
  - PRs that mix behavior change and refactoring with no test run in between;
  - "don't touch" areas of code;
  - well-covered but still tangled classes nobody refactors.
- **Recommended Action:** Make sure the behavior is covered first (add tests, PCC-290), change one small thing, run the suite, and repeat. After refactoring the tests themselves, run the suite again.
- **Exceptions/Trade-offs:** Coverage is not cleanliness: the improving still has to be done.
- **Related Rules:** PCC-266, PCC-276, PCC-290; ch1 "Working code is not finished code".
- **Review Question:** Was each refactoring step small, and was it checked by a passing suite?

#### PCC-274
**Treat hard-to-test code as a design signal and fix the production code**
- **Source Chapter:** 15 — Writing testable code and clean tests (sections: Using testability as design feedback; Designing production code for testability)
- **Principle:**
  - Testing a class in isolation needs **seams**: boundaries along which it can be cut out of the project.
  - Difficulty writing a test is information, and it arrives before the test ever runs.
  - The fix belongs in the production code, not in the test.
  - A class a test can separate is a class anything can separate: it can be swapped for another implementation, even at runtime.
  - A mock injected in a test is the same mechanism as a different implementation injected in production.
- **Problem:** Without seams, a test drags half the application in and is no longer a unit test. The same rigidity makes later requirement changes painful.
- **Detection Signals:**
  - the test needs more than the class under test plus mocked interfaces to run;
  - infrastructure errors (for example a DB connection failure) in a test that passed only mocks;
  - the class `new`s its own collaborators;
  - static calls to storage, files or the clock;
  - tests that need global configuration.
- **Recommended Action:** Introduce interfaces and constructor injection (PCC-286, PCC-288), remove static calls to controllable resources (PCC-285), and remove hidden work from constructors (PCC-289).
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-267, PCC-275, PCC-285–PCC-289; ch9 DIP; ch10 "Keeping the dependency graph visible".
- **Review Question:** Could this class be tested with only mocked interfaces, and if not, was the production code changed rather than the test bent?

#### PCC-275
**Use test-first (TDD) to settle the API from the caller's side**
- **Source Chapter:** 15 — Writing testable code and clean tests (section: Using test-driven development)
- **Principle:** Write a test for one small behavior that does not exist yet, write just enough code to pass it, improve the code while it stays green, then move to the next behavior.
  - Example: test that dividing 8 by 4 gives 2; then a test that a zero divisor throws, and only then implement that case.
  - Writing the test first fixes the signature, parameter order, return value and dependencies from the caller's view.
  - Needing to control data (for example from a database) pushes the design toward an abstraction and an injected dependency.
- **Problem:** Not phrased as a problem in the book. Implied: internals get designed before usage, and controllability questions arrive late, when design changes cost more.
- **Detection Signals:**
  - APIs that are awkward to call from a test;
  - edge-case branches (such as a zero divisor) with no test that demanded them;
  - feature commits with no accompanying tests.
- **Recommended Action:** Drive each new behavior with a failing test first, then the minimal code, then cleanup under the green suite.
- **Exceptions/Trade-offs:** The author stresses the benefit comes from constant feedback, "not because tests magically produce good code". She does not make TDD mandatory and does not say when to skip it (not stated in book).
- **Related Rules:** PCC-274, PCC-273, PCC-288.
- **Review Question:** Was this behavior's public shape decided by a test written from the caller's point of view?

#### PCC-276
**Treat test code with the same care as production code**
- **Source Chapter:** 15 — Writing testable code and clean tests (section: Treating test code as production code)
- **Principle:** The suite is part of the codebase and changes with it. It deserves the same attention to naming, structure, duplication and readability.
- **Problem:** Confusing or brittle tests become something the team maintains rather than something that helps. Tests that fail too often get disabled, the safety net loses its value, and trust erodes.
- **Detection Signals:**
  - `[Ignore]`, `Skip =` or commented-out tests, especially ones disabled for "failing too often";
  - copy-pasted tests;
  - generic names;
  - very long test methods;
  - test projects excluded from review or analyzers.
- **Recommended Action:** Review and refactor tests like production code by applying PCC-277–PCC-284, and run the suite after test refactoring.
- **Exceptions/Trade-offs:** Some conventions legitimately differ for tests (underscored names, PCC-277).
- **Related Rules:** PCC-273, PCC-277–PCC-284.
- **Review Question:** Would this test code pass the same review bar as the production code it covers?

#### PCC-277
**Name each test by method, expected result and scenario, using one convention**
- **Source Chapter:** 15 — Writing testable code and clean tests (sections: Writing readable and well-named tests; case study, Identifying the issues; Table 15.2 "Naming")
- **Principle:**
  - The name is often the first thing seen when a test fails.
  - It should state what is tested, the expected behavior and the scenario (for example `Divide_ShallGive_2_WhenDividing_8_By_4`).
  - The order of the parts is a team convention; **consistency matters more than any one pattern**.
  - Long names are acceptable. Underscores are fine because test names are read in reports, not called from code.
- **Problem:** Generic names (`Test1`, `BubbleSortTest`) say nothing when a test fails. Inconsistent patterns make the test explorer hard to read (the case study had three patterns among five tests, and only three started with the method name).
- **Detection Signals:**
  - names like `Test1`, `<Method>Test` or `TestSomething`;
  - names missing the scenario or the expected result;
  - mixed naming patterns within one fixture;
  - names that do not match what the test asserts.
- **Recommended Action:** Pick one pattern (the case study uses `Method_ShallExpectedResult_FromScenario`) and rename every test in the fixture to it.
- **Exceptions/Trade-offs:** Breaking normal method-naming rules with underscores is deliberate and fine for tests.
- **Related Rules:** PCC-276, PCC-280, PCC-292; ch2 "Long test names are often useful".
- **Review Question:** Does each test name alone tell you the method, the scenario and the expected outcome, in the same pattern as its neighbours?

#### PCC-278
**Keep tests short and move repeated setup into a shared place**
- **Source Chapter:** 15 — Writing testable code and clean tests (section: Writing readable and well-named tests; Table 15.2 "Length", "Duplication")
- **Principle:** A test should show what it checks at a glance, and short tests do that. Objects and configuration that many tests share go into a method the framework runs before each test, or into shared helpers.
- **Problem:** Long tests make the reader scroll through extensive setup to find the point.
- **Detection Signals:**
  - test bodies that need scrolling;
  - the same object graph or configuration built at the top of many tests in one fixture.
- **Recommended Action:** Use the framework's per-test setup (NUnit `[SetUp]`; xUnit: the test-class constructor; TUnit: a before-test hook — the last two are equivalents, not from the book) or shared helper methods.
- **Exceptions/Trade-offs:** The book qualifies this with "where appropriate" but does not say when shared setup hurts (not stated in book).
- **Related Rules:** PCC-276, PCC-279, PCC-282.
- **Review Question:** Is each test short enough to grasp at a glance, with shared setup moved out of its body?

#### PCC-279
**Keep arrange, act and assert visibly separate, with one cycle per test**
- **Source Chapter:** 15 — Writing testable code and clean tests (sections: Separating arrange, act, and assert; case study, Refactoring the overgrown test; Table 15.2 "Structure")
- **Principle:** A unit test has three conceptual steps: arrange data and objects, act on the behavior, assert the outcome. Keep them visually distinct; blank lines are usually enough. Explicit `// Arrange / Act / Assert` comments are optional, and if a team uses them it should use them consistently.
- **Problem:** Interleaving setup, execution and assertions makes tests hard to follow. Repeated AAA blocks in one method mean several scenarios in one test. Inconsistent AAA comments add length without clarity.
- **Detection Signals:**
  - assertions between act calls;
  - more than one act in a test;
  - repeated `// Arrange … // Act … // Assert` triplets in one method;
  - AAA comments present in some tests and missing in others in the same fixture.
- **Recommended Action:**
  - reorder each test into three blocks separated by blank lines;
  - split multiple cycles into separate or parameterized tests (PCC-280, PCC-282);
  - drop AAA labels from short tests where the structure is obvious.
- **Exceptions/Trade-offs:** AAA comments are acceptable as a consistent team convention. In very short tests they add nothing.
- **Related Rules:** PCC-280, PCC-282; ch14 comments.
- **Review Question:** Can you point to exactly one arrange, one act and one assert block in this test?

#### PCC-280
**Test one coherent behavior per test**
- **Source Chapter:** 15 — Writing testable code and clean tests (sections: Keeping tests focused; case study, Identifying the issues; Table 15.2 "Responsibility")
- **Principle:** Each test verifies one coherent behavior, so a failure points to one responsibility.
- **Problem:** A test that creates, reverses, orders and filters a collection before checking one value does not show which operation failed. In the case study, `BubbleSortTest` packed three cases; its name could not tell which one broke, and you had to count assertions to find out.
- **Detection Signals:**
  - several act calls on different operations;
  - numbered variables (`result1`, `result2`, `result3`);
  - a name too generic to describe the contents;
  - several AAA cycles in one test.
- **Recommended Action:** Split into separate tests. When the behavior is the same and only the data differs, parameterize (PCC-282).
- **Exceptions/Trade-offs:** Several assertions on one tightly connected result are fine (PCC-283).
- **Related Rules:** PCC-277, PCC-279, PCC-282, PCC-283.
- **Review Question:** If this test fails, is there exactly one behavior that could be responsible?

#### PCC-281
**Keep loops, conditionals and exception handling out of test bodies**
- **Source Chapter:** 15 — Writing testable code and clean tests (sections: Keeping tests focused; What unit tests do and do not prove)
- **Principle:** Control flow in a test adds behavior to the test itself. A test should have no logic in its body.
- **Problem:** Extra behavior in the test makes it harder to read and to trust, and complicated control flow usually means the test should be simplified or split.
- **Detection Signals:**
  - `for` / `foreach` / `while`, `if` / `switch` / ternaries, or `try` / `catch` inside a test method;
  - assertions inside branches.
- **Recommended Action:** Simplify or split the test. For expected exceptions, use the framework's exception assertion (the case study asserts that a call with null input throws `ArgumentNullException`) rather than try/catch.
- **Exceptions/Trade-offs:** A small loop can be reasonable when it makes the same assertion for every item in a collection.
- **Related Rules:** PCC-280, PCC-272.
- **Review Question:** Is this test free of branching and exception-handling logic, apart from at most a trivial per-item assertion loop?

#### PCC-282
**Replace copy-pasted tests with parameterized tests**
- **Source Chapter:** 15 — Writing testable code and clean tests (sections: Removing duplication with parameterized tests; case study, Refactoring the overgrown test; Table 15.2 "Duplication")
- **Principle:** Write the test body once with the values as parameters and declare the value sets. The framework runs and reports each set separately, so a failure still identifies the case (NUnit `[TestCase]`; equivalents not from the book: xUnit `[Theory]` + `[InlineData]`, TUnit `[Arguments]`).
- **Problem:** Duplicated test bodies with different literals mean every fix has to be made twice or more.
- **Detection Signals:**
  - two or more test methods with identical bodies and different literals;
  - the same three lines repeated in one test with different numbers.
- **Recommended Action:** Merge into one parameterized test whose name states the general expectation (the case study uses `…ShallProduceListSortedAscendingly_FromUnsortedInput` with two value sets).
- **Exceptions/Trade-offs:** The book states no rule here, but its case study keeps special cases (empty input, single item, already sorted, duplicates) as separate, individually named tests rather than folding them into the parameterized one.
- **Related Rules:** PCC-278, PCC-280, PCC-291; ch13 DRY.
- **Review Question:** Are tests that differ only in data written once as a parameterized test?

#### PCC-283
**Center each test on one logical outcome (not "one assert" dogma)**
- **Source Chapter:** 15 — Writing testable code and clean tests (section: Writing meaningful assertions)
- **Principle:** Assertions decide pass or fail, so they should verify the facts that matter. The practical default is one logical outcome per test. Several assertions are fine when they describe one tightly connected result; the example is a reversed list's count plus each element.
- **Problem:** (not stated in book beyond the focus argument in PCC-280)
- **Detection Signals:** assertions on unrelated outcomes in one test, for example results of different operations or unrelated properties.
- **Recommended Action:** Split unrelated assertions into separate tests. Keep grouped assertions only when together they describe one result.
- **Exceptions/Trade-offs:** Do not mechanically enforce one assertion per test.
- **Related Rules:** PCC-280, PCC-284.
- **Review Question:** Do all assertions in this test describe a single logical outcome?

#### PCC-284
**Do not assert details the behavior does not promise**
- **Source Chapter:** 15 — Writing testable code and clean tests (section: Writing meaningful assertions; Table 15.2 "Assertions")
- **Principle:** Assert only the facts relevant to the behavior under test.
- **Problem:** Asserting incidental details makes the test brittle without adding confidence. Example: a method that only selects a day in a given year, tested by also asserting the time components.
- **Detection Signals:**
  - full `DateTime` equality where only the date is the contract;
  - whole-object equality that includes fields the method does not set;
  - assertions on values the method under test does not produce or control.
- **Recommended Action:** Narrow each assertion to the promised fact.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-283, PCC-270, PCC-276.
- **Review Question:** Would this assertion survive a change that keeps the method's promised behavior intact?

#### PCC-285
**Do not reach controllable resources through static calls; leave pure statics alone**
- **Source Chapter:** 15 — Writing testable code and clean tests (section: Avoiding static dependencies that must be replaced; Table 15.3 "Static dependency")
- **Principle:** A static call removes the substitution point that an injected interface would give. That only matters when the static call reaches something a test needs to control: a database, a file, the network or the system clock.
- **Problem:** The test cannot substitute the behavior, so it must hit the real resource (slow, not isolated, not repeatable).
- **Detection Signals:**
  - static repository or data-access calls (for example `PeopleRepository.GetAll()`) inside logic;
  - `File.*` / `Directory.*`, static HTTP or network helpers, `DateTime.Now` / `DateTime.UtcNow` inside business logic;
  - static singletons or service locators fronting storage.
- **Recommended Action:** Introduce an injectable abstraction for the resource when it must vary or be controlled, and inject it (PCC-286, PCC-288). Ch10 covers wrapping static framework APIs.
- **Exceptions/Trade-offs:** Not every static call is a problem. Pure maths and simple text processing (trimming, case changes) are fast and predictable with nothing to substitute, so let the real code run.
- **Related Rules:** PCC-267, PCC-268, PCC-286; ch10 "Wrapping static framework APIs", "Keeping the dependency graph visible".
- **Review Question:** Does any static call in this class reach a database, file, network or clock that a test would need to control?

#### PCC-286
**Depend on an abstraction when a collaborator varies or must be controlled in tests**
- **Source Chapter:** 15 — Writing testable code and clean tests (section: Depending on abstractions rather than concrete types; Table 15.3 "Concrete dependency")
- **Principle:** The abstraction that DIP calls for becomes the test's substitution seam. The class states the capability it needs without committing to one implementation. The application passes the real repository and the test passes a mock.
- **Problem:** A concrete parameter such as `PeopleRepository` invites no substitute. The test then runs against the real repository and the real database: slow, data-dependent, and not a test of this class alone.
- **Detection Signals:**
  - constructor parameters or fields typed as concrete infrastructure classes (`…Repository`, `Sql…`, `File…`, `Http…`);
  - an interface exists but the consumer still takes the concrete type.
- **Recommended Action:** Change the field and parameter type to an interface (extract one if needed) and keep the implementation behind it.
- **Exceptions/Trade-offs:** The book qualifies this: apply it **when the collaborator is intended to vary or be replaced in tests**, not to every type. See ch13 "Avoiding premature abstraction" (topic).
- **Related Rules:** PCC-269, PCC-287, PCC-288; ch9 DIP.
- **Review Question:** Is every collaborator that must be controlled in tests typed as an abstraction?

#### PCC-287
**Mock interfaces, not concrete classes**
- **Source Chapter:** 15 — Writing testable code and clean tests (sections: Depending on abstractions rather than concrete types; Keeping constructors simple, fourth reason)
- **Principle:** Some mocking libraries fake a concrete class by generating a subclass at runtime. That works only for members the language lets you override. For the others, libraries either refuse or accept the setup and then ignore it, so the **real method runs while the test looks mocked**. Interfaces are the default approach.
- **Problem:**
  - silently real behavior inside a supposed mock;
  - tests tied to one implementation rather than a contract, so a change inside one class breaks another class's tests;
  - creating a subclass mock runs the base constructor, so if that constructor does real work the mock does too, and the test suddenly needs a database.
- **Detection Signals:**
  - `new Mock<SomeConcreteClass>()` (Moq) or the equivalent in other libraries;
  - setups on non-virtual members;
  - mocks of classes with heavy constructors;
  - tests needing infrastructure although only mocks are used.
- **Recommended Action:** Extract or use an interface for the contract and mock that.
- **Exceptions/Trade-offs:** Mocking a concrete class is "possible, although not recommended". It is a strong default, not an absolute ban.
- **Related Rules:** PCC-286, PCC-289.
- **Review Question:** Are all mocks in these tests created from interfaces rather than concrete classes?

#### PCC-288
**Inject collaborators through the constructor instead of creating them inside**
- **Source Chapter:** 15 — Writing testable code and clean tests (section: Injecting dependencies instead of creating them internally; Table 15.3 "Dependency created internally")
- **Principle:** An interface-typed field is not enough if the class constructs the concrete implementation itself. Constructor injection exposes the dependency and guarantees every valid instance receives it at creation.
- **Problem:** The test cannot supply a mock even though an interface exists. Publicly settable dependency fields would allow replacement, but they weaken encapsulation and let the dependency later be set to an invalid value.
- **Detection Signals:**
  - `_dep = new ConcreteDep()` in a constructor or field initializer;
  - `new …Repository()` / `new …Service()` inside methods;
  - parameterless constructors that build their own collaborators;
  - `public IXxx Dep { get; set; }` used as the injection mechanism.
- **Recommended Action:** Add a constructor parameter typed as the abstraction and assign it to a `readonly` field. The caller (production) supplies the real implementation; the test supplies a mock.
- **Exceptions/Trade-offs:**
  - Setter or field injection works but is the weaker option.
  - DI containers are not discussed (not stated in book).
  - See ch9 "Dependency Inversion and Dependency Injection are not the same" and ch9 "Creating dependencies only when runtime data is available" (factories) for collaborators that cannot exist at construction time.
- **Related Rules:** PCC-274, PCC-286, PCC-289; ch9.
- **Review Question:** Does the class receive every collaborator through its constructor, with none created internally or exposed through a public setter?

#### PCC-289
**Keep constructors trivial: assign, allocate, guard, and nothing more**
- **Source Chapter:** 15 — Writing testable code and clean tests (section: Keeping constructors simple; Table 15.3 "Complex constructor")
- **Principle:** Creating an object should be cheap and predictable. A constructor should only:
  - assign arguments to fields;
  - create simple structures the object owns (for example an empty list);
  - reject plainly invalid arguments (for example a null dependency).

  Never put these in a constructor: DB connections, file reads, web-service calls, loops, branching business logic, or anything expensive enough that you would want to see it happen. Work worth thinking about should be a method someone calls on purpose.
- **Problem:** The author gives four reasons, plus the general one that such failures are hard to trace because nobody looks at object creation:
  1. **Expectation:** nobody expects a constructor call to open a connection or need global configuration.
  2. **Performance:** creating 1 000 objects in a loop repeats the work 1 000 times.
  3. **Invisible timing:** constructors run where no `new` is visible, such as inside `Lazy<T>` on first use.
  4. **Inheritance:** base constructors run for derived types, including subclass-based mocks of concrete classes.

  Example: a `HousesStateUpdater` test with a mocked repository failed with a DB-connection error, because the `House` constructor created a real repository to check that the house and owner exist.
- **Detection Signals:**
  - constructors containing `new …Repository()`, connection or `.Open(` calls, `File.`, HTTP calls, global config reads;
  - loops or if/else business validation in constructors;
  - entity or value-type constructors that query storage;
  - `Lazy<T>` wrapping types with heavy constructors;
  - tests failing with infrastructure errors at object creation.
- **Recommended Action:** Move the real work into explicit methods or collaborators that are called deliberately. The book does not show a refactored `House`; it only prescribes this move.
- **Exceptions/Trade-offs:** Argument guard clauses for plainly invalid input belong in the constructor. "Simple" does not mean "no validation".
- **Related Rules:** PCC-266, PCC-287, PCC-288; ch2 "Following the principle of least surprise" (topic); ch9 "Creating dependencies only when runtime data is available".
- **Review Question:** Does this constructor do anything beyond assigning fields, allocating owned structures and rejecting invalid arguments?

#### PCC-290
**Cover every promised behavior, including guard clauses and regression-prone contracts**
- **Source Chapter:** 15 — Writing testable code and clean tests (sections: case study, Identifying the issues; Adding missing behavior checks; Table 15.4)
- **Principle:** Cleaning up existing tests is not enough if important branches stay uncovered. Behavior the code explicitly implements, such as rejecting null input, needs a test. A contract that an earlier version broke, such as returning a new list instead of sorting the input in place, deserves a dedicated regression test.
- **Problem:** Uncovered behavior can regress silently.
- **Detection Signals:**
  - `ArgumentNullException.ThrowIfNull` or other guard clauses with no test expecting the throw;
  - methods meant not to mutate their input with no test checking the input afterwards;
  - comments or history mentioning a behavior change ("modified to not modify the input") with no matching test;
  - bug fixes without a regression test.
- **Recommended Action:**
  - add a test asserting the guard throws, using the framework's exception assertion;
  - add a test that calls the method and asserts the original input is unchanged;
  - run the suite.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-273, PCC-281, PCC-291.
- **Review Question:** Does every explicit guard and every previously broken contract in this class have its own test?

#### PCC-291
**Remove redundant test cases**
- **Source Chapter:** 15 — Writing testable code and clean tests (sections: case study, Identifying the issues; Refactoring the overgrown test)
- **Principle:** When a scenario is already covered by a dedicated, well-named test, drop the duplicate case from other tests.
- **Problem:** Thinly stated. The book only calls the case redundant, because the already-sorted input in the multi-case test duplicated a dedicated test; the implied cost is extra length and maintenance.
- **Detection Signals:** the same input scenario asserted in two tests (often inside a multi-case "catch-all" test).
- **Recommended Action:** Delete the duplicate case and keep the dedicated test.
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-280, PCC-282.
- **Review Question:** Is each scenario in this fixture tested exactly once?

#### PCC-292
**Write the suite so it reads as the class's documentation**
- **Source Chapter:** 15 — Writing testable code and clean tests (sections: What unit tests do and do not prove; case study conclusion)
- **Principle:** Each test states one behavior, its inputs and its result, so opening a class's tests is often the fastest way to learn what the class does. Unlike a comment or a wiki page, a test cannot quietly go out of date: it fails.
- **Problem:** Not stated as a problem. Implied: prose documentation drifts from behavior (ch14).
- **Detection Signals:**
  - you cannot tell what the class does from its test names;
  - important behavior is documented only in comments or wiki.
- **Recommended Action:** Use focused, consistently named tests (PCC-277, PCC-280) and add tests for undocumented behaviors (PCC-290).
- **Exceptions/Trade-offs:** (not stated in book)
- **Related Rules:** PCC-277, PCC-280, PCC-290; ch14 "Outdated and misleading comments".
- **Review Question:** Could a newcomer learn this class's behavior by reading only its test names?

#### PCC-293
**When cleaning a class with its tests, strip comment clutter and keep only useful pointers**
- **Source Chapter:** 15 — Writing testable code and clean tests (sections: case study, Identifying the issues; Improving the code; Table 15.4)
- **Principle:** What stays on screen should be the code that runs.
  - Delete comments that restate the code and loop-end navigation markers.
  - Change history belongs in version control, and so does commented-out old code; recover it from the repository if ever needed.
  - One short link to an external description of the algorithm is worth keeping instead of a pasted explanation.
  - Rename cryptic locals so their comment becomes unnecessary (`n` → `itemCount`).
- **Problem:** Clutter hides the algorithm, and commented-out code leaves doubt about whether it still matters.
- **Detection Signals:**
  - comments like `// check if input is null` above a null guard;
  - `// end of inner loop`;
  - dated "implemented on / modified on" comments;
  - commented-out methods;
  - single-letter locals explained by a trailing comment.
- **Recommended Action:** Delete the clutter, rename the variable, keep one reference link, then run the tests.
- **Exceptions/Trade-offs:** Not every comment goes. A pointer to an algorithm's description earns its place (ch14 "Documenting complex algorithms").
- **Related Rules:** PCC-276; ch14 (restating, navigation, change-history, commented-out code); ch2 names.
- **Review Question:** Is every remaining comment something the code itself cannot say?

### Code smells & anti-patterns catalogue

| Name | Signal | Rule IDs |
|---|---|---|
| Manual verification | Behavior checked by clicking through the app or by a checklist, not a framework test | PCC-265 |
| Slow unit suite | Minutes-long runs; real DB/file/network; `Thread.Sleep` in unit tests | PCC-266 |
| Cross-class test breakage | Changing class A turns class B's tests red | PCC-267, PCC-274 |
| Environment- or data-dependent test | Expected values depend on DB contents, machine paths or the clock | PCC-267, PCC-268 |
| Order-dependent test | Passes only alone or in a particular order; shared static mutable state | PCC-268, PCC-272 |
| Real infrastructure in a unit test | Real repository / DB context / HTTP client constructed in the test | PCC-269 |
| Unchecked action-only method | `void` Save/Notify/Show with no test, or a test with no assertion | PCC-270 |
| "Green unit suite = app works" | No integration/E2E test for new wiring; everything mocked | PCC-271 |
| Unreadable integration test | Generic name, several scenarios, logic, shared state in a slow test | PCC-272 |
| Untouchable code / big-bang refactor | Large refactoring with no covering tests; "don't touch" areas | PCC-273 |
| Tests as cosmetic coverage | Well-covered but still tangled design nobody improves | PCC-273 |
| Missing seam | Test needs half the app or global config; infra errors despite mocks | PCC-274 |
| Awkward-from-caller API | Hard to call from a test; untested edge branches | PCC-275 |
| Disabled / ignored tests | `[Ignore]`, `Skip=`, commented-out tests "because they fail too often" | PCC-276 |
| Generic test name | `Test1`, `BubbleSortTest`, `<Method>Test` | PCC-277 |
| Inconsistent naming patterns | Several naming orders within one fixture | PCC-277 |
| Long test / duplicated setup | Scrolling test bodies; same setup at the top of many tests | PCC-278 |
| Interleaved or repeated AAA | Asserts between acts; several AAA cycles in one method | PCC-279, PCC-280 |
| Noisy or inconsistent AAA comments | `// Arrange/Act/Assert` in some tests, missing in others, in trivial tests | PCC-279 |
| Overgrown multi-scenario test | `result1/result2/result3`; a chain of unrelated operations before one check | PCC-280 |
| Logic in a test | `for`/`if`/`switch`/`try-catch` inside a test body | PCC-281 |
| Copy-paste tests | Identical bodies with different literals | PCC-282 |
| Unrelated assertions | One test asserting outcomes of different behaviors | PCC-283 |
| Incidental / brittle assertion | Full `DateTime` or whole-object equality beyond the promised fact | PCC-284 |
| Static call to a controllable resource | Static repository, `File.*`, `DateTime.Now`, static network helper in logic | PCC-285 |
| Concrete infrastructure dependency | Constructor parameter or field typed as a concrete repository/service | PCC-286 |
| Mocking a concrete class | `new Mock<ConcreteClass>()`; setups on non-virtual members | PCC-287 |
| Internally created dependency | `new ConcreteDep()` in a constructor, field initializer or method | PCC-288 |
| Public settable dependency | `public IDep Dep { get; set; }` as the injection mechanism | PCC-288 |
| Constructor doing work | Connections, file reads, web calls, loops or business validation in a constructor | PCC-289 |
| Hidden construction cost | `Lazy<T>` or a base constructor running heavy work out of sight | PCC-289, PCC-287 |
| Untested guard / missing regression test | `ThrowIfNull` without a throw test; a once-broken contract with no test | PCC-290 |
| Redundant test case | Same scenario asserted in two tests | PCC-291 |
| Restating / navigation / history comments | `// check if null`, `// end of loop`, dated change notes | PCC-293 |
| Commented-out old implementation | Dead methods kept in comments | PCC-293 |

### Refactoring techniques named in the chapter

- **Replace a real dependency with a mock** (mocking-library generated: configure what the method returns, pass the mock object into the constructor).
- **Verify an interaction on a mock** for action-only collaborators (the expected method was called with the expected arguments).
- **Depend on an abstraction** (change a concrete parameter or field type to an interface).
- **Constructor injection** (replace internal `new` with a constructor parameter assigned to a `readonly` field). Prefer it over public settable dependency fields.
- **Replace a static call with an injectable abstraction**, only for resources a test must control.
- **Move work out of the constructor** into explicit methods or collaborators called deliberately.
- **Small-step refactoring under tests:** rename a variable, extract a method, move a responsibility to another class, and run the suite after each step.
- **TDD cycle:** failing test for one small behavior → just enough code → improve while green → next behavior.
- **Hoist shared setup** into a per-test setup method (NUnit `[SetUp]`) or shared helpers.
- **Split an overgrown test** into focused tests.
- **Parameterize duplicated tests** (NUnit `[TestCase]`).
- **Remove redundant test cases** already covered by a dedicated test.
- **Rename tests to one consistent convention** (method / expected result / scenario).
- **Use blank lines instead of AAA comments** in short tests; keep AAA comments only as a consistent team convention.
- **Use the framework's exception assertion** instead of try/catch (asserting that a call throws `ArgumentNullException`).
- **Add missing behavior tests:** a guard-clause throw and a non-mutation-of-input regression test.
- **Delete comment clutter** (restating code, loop-navigation markers, change history, commented-out code), rely on version control, and keep a single link to an algorithm description.
- **Rename a cryptic local** to remove the need for its comment (`n` → `itemCount`).

### Things the author says NOT to do mechanically

- **Do not replace every static call.** Pure maths and simple text processing have nothing to substitute; letting them run for real is correct (PCC-285).
- **Do not introduce an interface for every type.** Abstract a collaborator only when it is meant to vary or be replaced in tests (PCC-286).
- **Mocking a concrete class is "not recommended", not forbidden.** Interfaces are the default because class mocks can silently run real code and run base constructors (PCC-287).
- **Do not enforce "one assert per test".** Several assertions are fine when they describe one tightly connected result (PCC-283).
- **Do not ban every loop in tests.** A small loop applying the same assertion to each item can be reasonable; complex control flow is the warning sign (PCC-281).
- **AAA comments are optional.** Blank lines usually suffice, comments add nothing in short tests, and if a team uses them it must be consistent (PCC-279).
- **There is no single correct test-naming order.** Consistency within the team matters more. Underscores and long names are acceptable for tests (PCC-277).
- **Shared setup only "where appropriate".** Hoisting is a tool for shortening tests, not a mandate (PCC-278).
- **Tests do not make code clean.** A big suite around a bad design leaves it bad; the refactoring still has to happen (PCC-273).
- **A green unit suite does not prove the application works.** Integration and E2E tests cover collaboration (PCC-271).
- **TDD's value comes from constant feedback, not magic.** The book presents TDD as a way to get design feedback early, not as a mandatory ritual, and gives no rule for when to skip it (PCC-275).
- **Do not fix testability by bending the test.** When a class is hard to test, the fix belongs in the production code (PCC-274).
- **"Simple constructor" does not mean "no validation".** Rejecting plainly invalid arguments such as a null dependency belongs there (PCC-289).
- **Not every comment should go.** A one-line link to the algorithm's description earns its place (PCC-293).

### Coverage gaps (not stated in book)

- No taxonomy of test doubles (mock vs stub vs fake vs spy). "Mock" is used for all of them.
- No discussion of DI containers or composition roots (only constructor vs setter injection). Ch9 covers DIP vs DI and factories.
- No code-coverage targets, mutation testing, test-data builders, async-test guidance, snapshot/approval tests, or rules on testing private methods.
- No explicit over-mocking or over-verification warning (call counts, call order). Only the general "assert only promised facts" rule.
- No handling of flaky tests other than noting that teams disable them, which destroys the safety net.
- No guidance on when *not* to use TDD.
- Figures 15.1–15.3 (the refactoring feedback loop and seam diagrams) appear only as captions.

