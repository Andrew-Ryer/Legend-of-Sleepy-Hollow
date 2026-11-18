# Legend-of-Sleepy-Hollow

## Git workflow

Use normal merges, not repeated squash merges, when synchronizing long-lived branches.
Normal merge commits record shared ancestry so later merges do not replay already-integrated changes.

Rider's `.idea/**/workspace.xml` is personal editor state and must stay untracked.
The existing `.idea/` ignore rule protects untracked settings, but cannot prevent conflicts while another branch still tracks them.
When integrating older branches, keep this file removed from version control; preserve local settings on disk.