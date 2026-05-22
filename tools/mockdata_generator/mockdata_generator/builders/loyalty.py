from __future__ import annotations


def build_saved_traveller_profiles(ctx) -> None:
    bookings = ctx.collections["Bookings"]
    items = []
    seen_pairs: set[tuple[str, str]] = set()
    for booking in bookings:
        for passenger in booking["passengers"]:
            pair = (booking["customerId"], passenger["fullName"])
            if pair in seen_pairs:
                continue
            seen_pairs.add(pair)
            items.append(
                {
                    "_id": ctx.factory.object_id(),
                    "customerId": booking["customerId"],
                    "fullName": passenger["fullName"],
                    "dateOfBirth": passenger["birthDate"],
                    "gender": passenger["gender"],
                    "passportNumber": passenger["passportNumber"],
                    "nationality": "Vietnamese",
                    "phone": booking["contactInfo"]["phone"],
                    "email": booking["contactInfo"]["email"],
                    "note": "" if len(items) % 3 else ctx.factory.sentence(),
                    "isDefault": len(items) % 4 == 0,
                    "createdAt": booking["createdAt"],
                    "updatedAt": ctx.factory.iso_timestamp(days_back=10),
                }
            )
            if len(items) >= ctx.counts["SavedTravellerProfiles"]:
                ctx.collections["SavedTravellerProfiles"] = items
                return

    customers = ctx.collections["Customers"]
    while len(items) < ctx.counts["SavedTravellerProfiles"] and customers:
        customer = customers[len(items) % len(customers)]
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "customerId": customer["_id"],
                "fullName": customer["fullName"],
                "dateOfBirth": ctx.factory.iso_timestamp(days_back=12000),
                "gender": ("Female", "Male")[len(items) % 2],
                "passportNumber": f"SAVED-{len(items) + 1:06d}",
                "nationality": "Vietnamese",
                "phone": customer["phoneNumber"],
                "email": customer["email"],
                "note": "",
                "isDefault": len(items) % 5 == 0,
                "createdAt": customer["createdAt"],
                "updatedAt": ctx.factory.iso_timestamp(days_back=10),
            }
        )
    ctx.collections["SavedTravellerProfiles"] = items


def build_loyalty_ledger_entries(ctx) -> None:
    completed_bookings = ctx.state.get("completed_bookings", [])
    if not completed_bookings:
        ctx.collections["LoyaltyLedgerEntrys"] = []
        return
    balances: dict[str, int] = {}
    items = []
    entry_types = ("Earn", "Bonus", "Redeem")
    for index in range(ctx.counts["LoyaltyLedgerEntrys"]):
        booking = completed_bookings[index % len(completed_bookings)]
        customer_id = booking["customerId"]
        entry_type = entry_types[index % len(entry_types)]
        delta = 120 if entry_type == "Earn" else (60 if entry_type == "Bonus" else -80)
        balances[customer_id] = balances.get(customer_id, 0) + delta
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "customerId": customer_id,
                "bookingId": booking["_id"],
                "type": entry_type,
                "title": f"{entry_type} points",
                "points": delta,
                "balanceAfter": balances[customer_id],
                "note": ctx.factory.sentence(),
                "createdAt": ctx.factory.iso_timestamp(days_back=50),
                "expiresAt": ctx.factory.future_date(60, 365) if entry_type != "Redeem" else None,
            }
        )
    ctx.collections["LoyaltyLedgerEntrys"] = items


def build_voucher_wallet_items(ctx) -> None:
    customers = ctx.collections["Customers"]
    promotions = ctx.collections["Promotions"]
    if not customers or not promotions:
        ctx.collections["VoucherWalletItems"] = []
        return
    items = []
    for index in range(ctx.counts["VoucherWalletItems"]):
        customer = customers[index % len(customers)]
        promotion = promotions[index % len(promotions)]
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "customerId": customer["_id"],
                "promotionId": promotion["_id"],
                "code": f"WALLET-{index + 1:05d}",
                "title": promotion["title"],
                "discountPercentage": promotion["discountPercentage"],
                "discountValue": promotion["discountValue"],
                "minimumSpend": promotion["minimumSpend"],
                "status": ("Active", "Used", "Expired")[index % 3],
                "source": promotion["campaignType"],
                "issuedAt": ctx.factory.iso_timestamp(days_back=35),
                "expiresAt": promotion["validTo"],
            }
        )
    ctx.collections["VoucherWalletItems"] = items
