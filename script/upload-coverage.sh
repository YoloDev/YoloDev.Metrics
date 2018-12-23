#!/bin/bash

curl -s https://codecov.io/bash >codecov
chmod +x codecov
./codecov -f "artifacts/coverage.opencover.xml" -t "${CODECOV_TOKEN}"
