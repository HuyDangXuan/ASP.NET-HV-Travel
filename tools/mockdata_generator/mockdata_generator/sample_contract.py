from __future__ import annotations

from collections import OrderedDict
from datetime import datetime, timezone
from decimal import Decimal
from typing import Any


_WRAPPER_KEYS = {"$oid", "$date", "$numberDecimal"}


def prepare_collections_for_export(collections: OrderedDict[str, list[dict]]) -> OrderedDict[str, list[dict]]:
    prepared = OrderedDict((name, items) for name, items in collections.items())

    if "Tours" in prepared:
        prepared["Tours"] = [_prepare_tour(item) for item in prepared["Tours"]]

    if "Customers" in prepared:
        prepared["Customers"] = [_prepare_customer(item) for item in prepared["Customers"]]

    if "Bookings" in prepared:
        prepared["Bookings"] = [_prepare_booking(item) for item in prepared["Bookings"]]

    return prepared


def is_wrapper_dict(value: Any) -> bool:
    return isinstance(value, dict) and len(value) == 1 and next(iter(value)) in _WRAPPER_KEYS


def _prepare_tour(tour: dict) -> dict:
    return {
        "_id": oid(tour["_id"]),
        "code": tour["code"],
        "name": tour["name"],
        "slug": tour["slug"],
        "description": tour["description"],
        "shortDescription": tour["shortDescription"],
        "seo": {
            "title": tour["seo"]["title"],
            "description": tour["seo"]["description"],
            "canonicalPath": tour["seo"]["canonicalPath"],
            "openGraphImageUrl": tour["seo"]["openGraphImageUrl"],
        },
        "destination": {
            "city": tour["destination"]["city"],
            "country": tour["destination"]["country"],
            "region": tour["destination"]["region"],
        },
        "images": list(tour["images"]),
        "price": {
            "adult": decimal_value(tour["price"]["adult"]),
            "child": decimal_value(tour["price"]["child"]),
            "infant": decimal_value(tour["price"]["infant"]),
            "currency": tour["price"]["currency"],
            "discount": tour["price"]["discount"],
        },
        "duration": {
            "days": tour["duration"]["days"],
            "nights": tour["duration"]["nights"],
            "text": tour["duration"]["text"],
        },
        "startDates": [date_value(start_date) for start_date in tour["startDates"]],
        "departures": [
            {
                "id": item["id"],
                "startDate": date_value(item["startDate"]),
                "adultPrice": decimal_value(item["adultPrice"]),
                "childPrice": decimal_value(item["childPrice"]),
                "infantPrice": decimal_value(item["infantPrice"]),
                "discountPercentage": item["discountPercentage"],
                "capacity": item["capacity"],
                "bookedCount": item["bookedCount"],
                "confirmationType": item["confirmationType"],
                "status": item["status"],
                "cutoffHours": item["cutoffHours"],
            }
            for item in tour["departures"]
        ],
        "schedule": [
            {
                "day": item["day"],
                "title": item["title"],
                "description": item["description"],
                "activities": list(item["activities"]),
            }
            for item in tour["schedule"]
        ],
        "highlights": list(tour["highlights"]),
        "generatedInclusions": list(tour["generatedInclusions"]),
        "generatedExclusions": list(tour["generatedExclusions"]),
        "cancellationPolicy": {
            "summary": tour["cancellationPolicy"]["summary"],
            "isFreeCancellation": tour["cancellationPolicy"]["isFreeCancellation"],
            "freeCancellationBeforeHours": tour["cancellationPolicy"]["freeCancellationBeforeHours"],
        },
        "confirmationType": tour["confirmationType"],
        "meetingPoint": tour["meetingPoint"],
        "badgeSet": list(tour["badgeSet"]),
        "routing": _prepare_tour_routing(tour.get("routing")),
        "maxParticipants": tour["maxParticipants"],
        "currentParticipants": tour["currentParticipants"],
        "rating": tour["rating"],
        "reviewCount": tour["reviewCount"],
        "createdAt": date_value(tour["createdAt"]),
        "updatedAt": date_value(tour["updatedAt"]),
        "version": int(tour["version"]),
        "status": tour["status"],
    }


def _prepare_tour_routing(routing: dict | None) -> dict | None:
    if routing is None:
        return None

    return {
        "schemaVersion": routing["schemaVersion"],
        "stops": [
            {
                "id": item["id"],
                "day": item["day"],
                "order": item["order"],
                "name": item["name"],
                "type": item["type"],
                "coordinates": {
                    "lat": item["coordinates"]["lat"],
                    "lng": item["coordinates"]["lng"],
                },
                "visitMinutes": item["visitMinutes"],
                "attractionScore": item["attractionScore"],
                "note": item["note"],
            }
            for item in routing["stops"]
        ],
    }


def _prepare_customer(customer: dict) -> dict:
    return {
        "_id": oid(customer["_id"]),
        "customerCode": customer["customerCode"],
        "fullName": customer["fullName"],
        "email": customer["email"],
        "password": customer["password"],
        "phoneNumber": customer["phoneNumber"],
        "avatarUrl": customer["avatarUrl"],
        "address": {
            "street": customer["address"]["street"],
            "city": customer["address"]["city"],
            "country": customer["address"]["country"],
        },
        "notes": customer["notes"],
        "segment": customer["segment"],
        "status": customer["status"],
        "emailVerified": customer["emailVerified"],
        "tokenVersion": customer["tokenVersion"],
        "stats": {
            "lifetimeSpend": customer["stats"]["lifetimeSpend"],
            "tripCount": customer["stats"]["tripCount"],
            "loyaltyPoints": customer["stats"]["loyaltyPoints"],
            "pendingPoints": customer["stats"]["pendingPoints"],
            "tier": customer["stats"]["tier"],
            "referralCode": customer["stats"]["referralCode"],
            "voucherBalance": customer["stats"]["voucherBalance"],
            "lastActivity": date_value(customer["stats"]["lastActivity"]),
            "lastCompletedTripAt": date_value(customer["stats"]["lastCompletedTripAt"]),
        },
        "tags": list(customer["tags"]),
        "createdAt": date_value(customer["createdAt"]),
        "updatedAt": date_value(customer["updatedAt"]),
    }


def _prepare_booking(booking: dict) -> dict:
    return {
        "_id": oid(booking["_id"]),
        "bookingCode": booking["bookingCode"],
        "tourId": oid(booking["tourId"]),
        "tourSnapshot": {
            "code": booking["tourSnapshot"]["code"],
            "name": booking["tourSnapshot"]["name"],
            "startDate": date_value(booking["tourSnapshot"]["startDate"]),
            "duration": booking["tourSnapshot"]["duration"],
        },
        "customerId": oid(booking["customerId"]),
        "bookingDate": date_value(booking["bookingDate"]),
        "totalAmount": decimal_value(booking["totalAmount"], force_one_decimal=True),
        "status": booking["status"],
        "paymentStatus": booking["paymentStatus"],
        "participantsCount": booking["participantsCount"],
        "passengers": [
            {
                "fullName": passenger["fullName"],
                "birthDate": date_value(passenger["birthDate"]),
                "type": passenger["type"],
                "gender": passenger["gender"],
                "passportNumber": passenger["passportNumber"],
            }
            for passenger in booking["passengers"]
        ],
        "contactInfo": {
            "name": booking["contactInfo"]["name"],
            "email": booking["contactInfo"]["email"],
            "phone": booking["contactInfo"]["phone"],
        },
        "notes": booking["notes"],
        "historyLog": [
            {
                "action": entry["action"],
                "timestamp": date_value(entry["timestamp"]),
                "user": entry["user"],
                "note": entry["note"],
            }
            for entry in booking["historyLog"]
        ],
        "events": [
            {
                "type": entry["type"],
                "title": entry["title"],
                "description": entry["description"],
                "occurredAt": date_value(entry["occurredAt"]),
                "actor": entry["actor"],
                "visibleToCustomer": entry["visibleToCustomer"],
            }
            for entry in booking["events"]
        ],
        "paymentTransactions": [
            {
                "provider": entry["provider"],
                "method": entry["method"],
                "transactionId": entry["transactionId"],
                "reference": entry["reference"],
                "amount": decimal_value(entry["amount"], force_one_decimal=True),
                "status": entry["status"],
                "receivedFromWebhook": entry["receivedFromWebhook"],
                "payloadHash": entry["payloadHash"],
                "createdAt": date_value(entry["createdAt"]),
                "processedAt": date_value(entry["processedAt"]),
            }
            for entry in booking["paymentTransactions"]
        ],
        "cancellationRequest": _prepare_cancellation_request(booking["cancellationRequest"]),
        "publicLookupEnabled": booking["publicLookupEnabled"],
        "receiptNumber": booking["receiptNumber"],
        "transferProofFileName": booking["transferProofFileName"],
        "transferProofContentType": booking["transferProofContentType"],
        "transferProofBase64": booking["transferProofBase64"],
        "confirmedAt": date_value(booking["confirmedAt"]),
        "completedAt": date_value(booking["completedAt"]),
        "createdAt": date_value(booking["createdAt"]),
        "updatedAt": date_value(booking["updatedAt"]),
        "isDeleted": booking["isDeleted"],
        "deletedBy": booking["deletedBy"],
        "deletedAt": date_value(booking["deletedAt"]),
    }


def _prepare_cancellation_request(value: dict | None) -> dict | None:
    if value is None:
        return None
    return {
        "status": value["status"],
        "reason": value["reason"],
        "requestedAt": date_value(value["requestedAt"]),
        "requestedBy": value["requestedBy"],
        "processedAt": date_value(value["processedAt"]),
        "resolutionNote": value["resolutionNote"],
    }


def oid(value: str | None) -> dict | None:
    if not value:
        return None
    return {"$oid": value}


def date_value(value: str | None) -> dict | None:
    if value is None:
        return None
    if is_wrapper_dict(value):
        return value
    dt = parse_utc(value)
    return {"$date": dt.astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.000Z")}


def decimal_value(value: Any, *, force_one_decimal: bool = False) -> dict:
    numeric = Decimal(str(value))
    if force_one_decimal:
        text = f"{numeric:.1f}"
    else:
        text = format(numeric.normalize(), "f")
        if "." in text:
            text = text.rstrip("0").rstrip(".")
    return {"$numberDecimal": text}


def parse_utc(value: str | None) -> datetime:
    if value is None:
        return datetime(2026, 3, 29, tzinfo=timezone.utc)
    text = value
    if text.endswith("Z"):
        text = text[:-1] + "+00:00"
    return datetime.fromisoformat(text).astimezone(timezone.utc)
