#!/usr/bin/env python3
"""Anonymize an IBKR Activity Flex XML export into a committable test fixture.

The scale factor is a required argument on purpose: it must never be committed.

    python3 tools/ibkr-fixtures/anonymize_flex.py \
        --scale "$IBKR_FIXTURE_SCALE" \
        private/ibrk_option_info_v3.xml \
        tests/fixtures/ibkr/ibkr-activity-v3.anon.xml

Rules (PBI-01 §11):
  1. every accountId -> --account (default U0000001); acctAlias cleared
  2. PII attributes removed (primaryEmail, dateOfBirth, names, reps, addresses ...)
  3. every money / price attribute multiplied by --scale with exact decimal math
     (no rounding); quantities, conids, strikes, symbols and dates are unchanged
  4. FlexStatement.whenGenerated fixed to --when-generated

Attributes are rewritten in place (regex over the raw text) so attribute order,
quoting and line layout of the original export are preserved.
"""

from __future__ import annotations

import argparse
import re
import sys
from decimal import Decimal, InvalidOperation, getcontext

getcontext().prec = 60

SCALED_ATTRIBUTES = frozenset(
    {
        "accruedInt",
        "amount",
        "changeInPrice",
        "closePrice",
        "commisionsAndTax",
        "cost",
        "costBasis",
        "costBasisMoney",
        "costBasisPrice",
        "fifoPnlRealized",
        "fifoPnlUnrealized",
        "fxPnl",
        "ibCommission",
        "initialInvestment",
        "markPrice",
        "mtmPnl",
        "netCash",
        "openPrice",
        "origTradePrice",
        "positionValue",
        "proceeds",
        "realizedPnl",
        "taxes",
        "tradeMoney",
        "tradePrice",
        "value",
    }
)

PII_ATTRIBUTES = frozenset(
    {
        "accountRepName",
        "accountRepPhone",
        "beneficiaryDesignations",
        "city",
        "country",
        "dateOfBirth",
        "firstName",
        "lastName",
        "masterName",
        "name",
        "postalCode",
        "primaryEmail",
        "state",
        "street",
        "street2",
        "streetResidentialAddress",
    }
)

ATTRIBUTE = re.compile(r'(\s)([A-Za-z][A-Za-z0-9]*)="([^"]*)"')


def format_decimal(value: Decimal) -> str:
    text = format(value, "f")
    if "." in text:
        text = text.rstrip("0").rstrip(".")
    return "0" if text in ("-0", "") else text


def scale_value(raw: str, scale: Decimal) -> str:
    if raw == "":
        return raw
    try:
        number = Decimal(raw)
    except InvalidOperation:
        return raw
    return format_decimal(number * scale)


def anonymize(text: str, scale: Decimal, account: str, when_generated: str) -> str:
    def replace(match: re.Match[str]) -> str:
        space, name, value = match.groups()
        if name in PII_ATTRIBUTES:
            return ""
        if name == "accountId":
            return f'{space}{name}="{account}"'
        if name == "acctAlias":
            return f'{space}{name}=""'
        if name == "whenGenerated":
            return f'{space}{name}="{when_generated}"'
        if name in SCALED_ATTRIBUTES:
            return f'{space}{name}="{scale_value(value, scale)}"'
        return match.group(0)

    return ATTRIBUTE.sub(replace, text)


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("source")
    parser.add_argument("target")
    parser.add_argument("--scale", required=True, help="money/price scale factor (never commit it)")
    parser.add_argument("--account", default="U0000001")
    parser.add_argument("--when-generated", default="20261117;120000")
    args = parser.parse_args(argv)

    scale = Decimal(args.scale)
    with open(args.source, encoding="utf-8", newline="") as handle:
        original = handle.read()
    real_accounts = set(re.findall(r'accountId="([^"]*)"', original)) - {"", args.account}

    result = anonymize(original, scale, args.account, args.when_generated)

    leaks = [account for account in real_accounts if account in result]
    if leaks or re.search(r"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", result):
        print("refusing to write: account id or e-mail address still present", file=sys.stderr)
        return 1

    with open(args.target, "w", encoding="utf-8", newline="") as handle:
        handle.write(result)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
