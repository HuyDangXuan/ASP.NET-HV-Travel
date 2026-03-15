function snakeToCamel(name) {
  if (!name || name.startsWith('_') || name.startsWith('$') || !name.includes('_')) {
    return name;
  }

  const parts = name.split('_').filter(Boolean);
  if (parts.length === 0) {
    return name;
  }

  return parts[0] + parts.slice(1).map(part => part.charAt(0).toUpperCase() + part.slice(1)).join('');
}

function normalizeValue(value) {
  if (Array.isArray(value)) {
    return value.map(normalizeValue);
  }

  if (value && typeof value === 'object' && !(value instanceof Date) && !(value._bsontype)) {
    const { normalized } = normalizeDocument(value);
    return normalized;
  }

  return value;
}

function normalizeDocument(doc) {
  const normalized = {};
  let changed = false;

  for (const [key, value] of Object.entries(doc)) {
    const normalizedKey = snakeToCamel(key);
    const normalizedValue = normalizeValue(value);

    if (normalizedKey !== key) {
      changed = true;
    }

    if (Object.prototype.hasOwnProperty.call(normalized, normalizedKey)) {
      continue;
    }

    normalized[normalizedKey] = normalizedValue;
    if (normalizedValue !== value) {
      changed = true;
    }
  }

  return { normalized, changed };
}

function migrateCollection(name) {
  const collection = db.getCollection(name);
  let scanned = 0;
  let updated = 0;

  collection.find({}).forEach(doc => {
    scanned += 1;
    const { normalized, changed } = normalizeDocument(doc);

    if (!changed) {
      return;
    }

    normalized._id = doc._id;
    collection.replaceOne({ _id: doc._id }, normalized);
    updated += 1;
  });

  print(`${name}: scanned=${scanned}, updated=${updated}`);
}

[
  'Users',
  'Tours',
  'Customers',
  'Bookings',
  'Payments',
  'Promotions',
  'Reviews',
  'Notifications'
].forEach(migrateCollection);
