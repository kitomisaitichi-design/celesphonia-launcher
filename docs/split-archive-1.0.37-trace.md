# Split-ZIP first-volume routing — launcher 1.0.37

## Complaint and scope

Grenim reported 7,283/11,738 exact files found for Ver1.061, with 4,455 missing, including creator-mod audio. Their original volume names/sizes and launcher version are unavailable. A full local Ver1.061 creator archive contains the reported audio; a base game alone cannot supply mod-specific replacements. Missing-file counts cannot identify which archive volume is absent. Do not assume that a continuation file should unpack independently.

## Reproduction and cause

A synthetic stored ZIP contains one Unicode-wrapped 6,000-byte deterministic file. Its bytes are split at 4,096 into sample.zip.001 and .002. Bundled 7-Zip lists/extracts both and restores payload SHA256 `79c06504d1d5c99c05c04495f8f53df7e0d1f28afd5cb13aa5a0970aaf47774d`. Compiled launcher 1.0.36 fails with `End of Central Directory record could not be found`; an ordinary ZIP control succeeds. Both archive classifiers prioritized the ZIP signature before the first-volume suffix, sending a partial ZIP into ZipFile.OpenRead.

## Correction

Both discovery and mixed-source recovery now route numeric first volumes through the existing bundled 7-Zip reader before testing ZIP magic. Ordinary/mislabelled single archives remain signature-based. Errors keep their failure/exit detail and add first-volume instructions. ZIP Unix symbolic-link attribute metadata is rejected on the new route, retaining the single-ZIP reader's link protection; traversal and case-collision checks remain. No dependency, source normalization, game edit or save migration change was introduced. Arbitrarily renamed individual native fragments, including .002, still match only exact native size/hash.

## Focused verification

`Tests/SplitArchiveTests.cs` runs against the actual old/new compiled launcher. It covers exact recovery, ordinary ZIP, mislabelled ZIP, standalone complete ZIP named .001, raw/uppercase first-volume names, split 7z, a native Map027 subset from the real Ver1.061 RAR, cancellation, source preservation, missing/corrupt/truncated continuation and traversal/duplicate/link rejection during recovery and discovery. Synthetic source hashes are frozen before execution. The initial baseline harness caught IOException but .NET emits InvalidDataException; the harness was corrected to catch both without changing production error handling.

Release gates also check the notes reader, existing launcher layouts and extracted package apply/update/restore. Existing updater size/entry limits remain unchanged. Original installed state/native/English-pack hashes are frozen and compared; the existing game/translation proof is reused because those components are unchanged. No full gameplay replay or exact replay of Grenim's unknown files is claimed. Before/after evidence is diagnostic, not a fabricated gameplay screenshot.

## User action

Update the launcher, place all matching volumes in one folder and select the first .001 from Add source files. For already extracted incomplete folders, add the complete original archive set alongside the base/shared originals. Keep final exact-file validation enabled. If files remain missing, obtain the complete matching original volumes and report their full filenames/sizes; the launcher cannot reconstruct bytes absent from all selected sources.
