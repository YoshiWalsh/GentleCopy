# GentleCopy

GentleCopy is a utility to perform bulk recursive file copy operations.
Interrupted copy operations can be resumed with no extra operations on
the source/destination filesystems via the use of an external log file.

I created this tool while trying to rescue data from a dying SSD. The
SSD only works for about 30 minutes at a time, and existing copy utilities
wasted too much time each attempt re-enumerating the files to be copied +
working out which files had already been copied.

GentleCopy even allows pre-loading the log/queue from a previous run while
the source and/or destination are unavailable, then waiting for a keypress
before attempting any operations. This allows reconnecting an unreliable source
and having GentleCopy ready to resume copying immediately.

## Usage

./GentleCopy.exe <sourcePath> <destinationPath> <pathToResumeFile>

GentleCopy will prepare its internal queue and then prompt for input. Connect
any required filesystems and then press enter to commence/resume operations.