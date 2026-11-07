# Character art and third-party rights

This independent application's C# source is under MIT. **That license does not cover any Capri artwork, character design, animation, voice, or marks** supplied by others.

The example seven animation files (idle, talk, sleep, glitch, tossed, perch and perch-music) supplied during development appear to match assets in https://github.com/lachydotmcg/capri-desktop. Their project README attributes animation art to an artist who prefers to remain private. That project uses the **PolyForm Noncommercial 1.0.0** license and includes required notices for reuse of its covered assets.

**Required Notice: Copyright 2026 Lachy (https://github.com/lachydotmcg)**

**Required Notice: Anyone who uses Capri's code, art, animations or any of its other assets must credit the Capri project (https://github.com/lachydotmcg/capri-desktop). Capri's art and animations are by her artist, who prefers to stay private. Parts made by others keep their own licences, listed in its README.**

License text: https://polyformproject.org/licenses/noncommercial/1.0.0/

The source repository still does not commit binary sprite frames, but the **Windows release build automatically packages default animations** from a pinned upstream source revision (currently `3d460f3a37ec0d2bb085b00541d1de6dc531f2ef`) under the separate **PolyForm Noncommercial 1.0.0** terms. This is not a claim of original authorship. Bundled artifacts include upstream LICENSE.md, THIRD-PARTY-NOTICES.md and a mandatory attribution notice inside `character-default/`. The source-code MIT license does not apply to these files.

This use relies on the upstream project's explicit noncommercial-sharing permission with required notices. **The application installer containing these art assets must be distributed noncommercially; do not charge for it or incorporate it into paid products without separate rights.** Some character and IP rights may be held by additional owners. The upstream artist's privacy is respected.

The repository intentionally contains **no original Capri fan art or converted third-party sprite frames**. The optional WebP importer processes files already on the user's PC. The *Windows CI build*, separately, retrieves pinned WebPs from the upstream repository to assemble its default art, and packages their license/credit notices with them. A separately supplied test pack should retain attribution and its applicable license notices.

**Distribution condition:** these public character-faithful Windows installers are for noncommercial use only, subject to the upstream required notices and any additional character/IP rights. This does not authorize paid redistribution, commercial products or reuse without attribution. Re-evaluate permissions before any commercial distribution; otherwise omit upstream art or obtain an appropriate license.

This is an unofficial fan project, unaffiliated with CD PROJEKT RED, Netflix or the creators of the anime.
