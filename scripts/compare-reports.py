"""Compare native read-feature3 JSON samples without assigning protocol semantics."""
import argparse
import json
from pathlib import Path
from itertools import zip_longest

def load_sample(path, sample):
    rows = json.loads(Path(path).read_text(encoding='utf-8-sig'))
    if not isinstance(rows, list) or not 1 <= sample <= len(rows):
        raise ValueError('Sample index out of range')
    return bytes.fromhex(rows[sample-1]['hexIncludingReportId'])

def compare(before, after):
    return {'beforeBytes': len(before), 'afterBytes': len(after),
            'changes': [{'offsetIncludingReportId': offset, 'before': a, 'after': b}
                        for offset,(a,b) in enumerate(zip_longest(before,after)) if a != b]}

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('before',type=Path)
    parser.add_argument('after',type=Path)
    parser.add_argument('--before-sample',type=int,default=1)
    parser.add_argument('--after-sample',type=int,default=1)
    args = parser.parse_args()
    print(json.dumps(compare(load_sample(args.before,args.before_sample),load_sample(args.after,args.after_sample)),indent=2))
